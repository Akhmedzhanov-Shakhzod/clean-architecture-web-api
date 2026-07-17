using CleanArchitecture.Application.Helpers.ApplicationExceptions;
using CleanArchitecture.Application.Helpers.DbContexts;
using CleanArchitecture.Application.Services;
using CleanArchitecture.Application.Services.Auth;
using CleanArchitecture.Application.Dtos.Auth;
using CleanArchitecture.Application.Dtos.Users;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Helpers.Jwts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Services.Auth.Impl
{
    public class AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IApplicationDbContext context,
        ITokenService tokenService,
        ICurrentUserService currentUser,
        TimeProvider timeProvider,
        ILogger<AuthService> logger) : IAuthService
    {
        public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            var existing = await userManager.FindByEmailAsync(request.Email);
            if (existing is not null)
                throw new ConflictException("A user with this email already exists.");

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim()
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(user, Roles.User);

            logger.LogInformation("New user registered: {UserId}", user.Id);

            return await IssueTokensAsync(user, cancellationToken);
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            // Одинаковая ошибка для «нет такого пользователя» и «неверный пароль» — защита от перебора аккаунтов.
            if (user is null)
                throw new UnauthorizedException("Invalid email or password.");

            if (!user.IsActive)
                throw new UnauthorizedException("Account is disabled.");

            var signIn = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (signIn.IsLockedOut)
                throw new UnauthorizedException("Account is temporarily locked due to too many failed attempts. Try again later.");

            if (!signIn.Succeeded)
                throw new UnauthorizedException("Invalid email or password.");

            await RemoveStaleRefreshTokensAsync(user.Id, cancellationToken);

            return await IssueTokensAsync(user, cancellationToken);
        }

        public async Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new UnauthorizedException("Refresh token is missing.");

            var tokenHash = tokenService.HashToken(refreshToken);
            var storedToken = await context.RefreshTokens
                .Include(t => t.User)
                .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

            if (storedToken is null)
                throw new UnauthorizedException("Invalid refresh token.");

            var now = timeProvider.GetUtcNow().UtcDateTime;

            if (storedToken.IsRevoked)
            {
                // Повторное использование отозванного токена — вероятная кража. Отзываем всю цепочку потомков.
                logger.LogWarning("Refresh token reuse detected for user {UserId}. Revoking descendant tokens.", storedToken.UserId);
                await RevokeDescendantsAsync(storedToken, now, "Ancestor token reuse detected", cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                throw new UnauthorizedException("Invalid refresh token.");
            }

            if (storedToken.IsExpired)
                throw new UnauthorizedException("Refresh token has expired.");

            if (!storedToken.User.IsActive)
                throw new UnauthorizedException("Account is disabled.");

            // Ротация: отзываем предъявленный токен и выпускаем новый, связанный с ним.
            var (rawToken, newHash, expiresAt) = tokenService.GenerateRefreshToken();

            storedToken.RevokedAt = now;
            storedToken.RevokedByIp = currentUser.IpAddress;
            storedToken.ReplacedByTokenHash = newHash;
            storedToken.ReasonRevoked = "Rotated";

            context.RefreshTokens.Add(new RefreshToken
            {
                UserId = storedToken.UserId,
                TokenHash = newHash,
                ExpiresAt = expiresAt,
                CreatedAt = now,
                CreatedByIp = currentUser.IpAddress
            });

            await context.SaveChangesAsync(cancellationToken);
            await RemoveStaleRefreshTokensAsync(storedToken.UserId, cancellationToken);

            var roles = await userManager.GetRolesAsync(storedToken.User);
            var (accessToken, accessExpiresAt) = tokenService.GenerateAccessToken(storedToken.User, roles);

            return new AuthResult
            {
                Response = new AuthResponse
                {
                    AccessToken = accessToken,
                    AccessTokenExpiresAt = accessExpiresAt,
                    User = MapUser(storedToken.User, roles)
                },
                RefreshToken = rawToken,
                RefreshTokenExpiresAt = expiresAt
            };
        }

        public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return;

            var tokenHash = tokenService.HashToken(refreshToken);
            var storedToken = await context.RefreshTokens
                .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

            if (storedToken is null || storedToken.IsRevoked)
                return;

            storedToken.RevokedAt = timeProvider.GetUtcNow().UtcDateTime;
            storedToken.RevokedByIp = currentUser.IpAddress;
            storedToken.ReasonRevoked = "Logged out";

            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<AuthResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
        {
            var user = await GetRequiredCurrentUserAsync();

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));

            // Безопасность: после смены пароля закрываем все существующие сессии.
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var activeTokens = await context.RefreshTokens
                .Where(t => t.UserId == user.Id && t.RevokedAt == null && t.ExpiresAt > now)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.RevokedAt = now;
                token.RevokedByIp = currentUser.IpAddress;
                token.ReasonRevoked = "Password changed";
            }

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Password changed for user {UserId}; {Count} refresh tokens revoked.", user.Id, activeTokens.Count);

            return await IssueTokensAsync(user, cancellationToken);
        }

        public async Task<UserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            var user = await GetRequiredCurrentUserAsync();
            var roles = await userManager.GetRolesAsync(user);

            return MapUser(user, roles);
        }

        private async Task<AuthResult> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var roles = await userManager.GetRolesAsync(user);
            var (accessToken, accessExpiresAt) = tokenService.GenerateAccessToken(user, roles);
            var (rawToken, tokenHash, refreshExpiresAt) = tokenService.GenerateRefreshToken();

            context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = refreshExpiresAt,
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
                CreatedByIp = currentUser.IpAddress
            });

            await context.SaveChangesAsync(cancellationToken);

            return new AuthResult
            {
                Response = new AuthResponse
                {
                    AccessToken = accessToken,
                    AccessTokenExpiresAt = accessExpiresAt,
                    User = MapUser(user, roles)
                },
                RefreshToken = rawToken,
                RefreshTokenExpiresAt = refreshExpiresAt
            };
        }

        private async Task RevokeDescendantsAsync(
            RefreshToken token, DateTime now, string reason, CancellationToken cancellationToken)
        {
            var current = token;
            while (current.ReplacedByTokenHash is not null)
            {
                var next = await context.RefreshTokens
                    .SingleOrDefaultAsync(t => t.TokenHash == current.ReplacedByTokenHash, cancellationToken);

                if (next is null)
                    break;

                if (next.IsActive)
                {
                    next.RevokedAt = now;
                    next.RevokedByIp = currentUser.IpAddress;
                    next.ReasonRevoked = reason;
                }

                current = next;
            }
        }

        /// <summary>Удаляет отозванные/просроченные токены старше окна хранения (одним SQL-запросом).</summary>
        private async Task RemoveStaleRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
        {
            var cutoff = timeProvider.GetUtcNow().UtcDateTime.AddDays(-30);

            await context.RefreshTokens
                .Where(t => t.UserId == userId &&
                            t.CreatedAt < cutoff &&
                            (t.RevokedAt != null || t.ExpiresAt < cutoff))
                .ExecuteDeleteAsync(cancellationToken);
        }

        private async Task<ApplicationUser> GetRequiredCurrentUserAsync()
        {
            if (currentUser.UserId is null)
                throw new UnauthorizedException();

            var user = await userManager.FindByIdAsync(currentUser.UserId.Value.ToString());

            return user ?? throw new UnauthorizedException("User no longer exists.");
        }

        private static UserDto MapUser(ApplicationUser user, IEnumerable<string> roles) => new()
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        };
    }
}
