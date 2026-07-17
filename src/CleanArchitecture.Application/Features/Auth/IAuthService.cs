using CleanArchitecture.Application.Features.Auth.Models;
using CleanArchitecture.Application.Features.Users.Models;

namespace CleanArchitecture.Application.Features.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Rotates the refresh token: revokes the presented one and issues a new pair.</summary>
    Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes the presented refresh token. Idempotent.</summary>
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Changes password and revokes every active refresh token, then issues a fresh pair.</summary>
    Task<AuthResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task<UserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
