using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CleanArchitecture.Infrastructure.Identity
{
    public class TokenService(IOptions<JwtSettings> jwtOptions, TimeProvider timeProvider) : ITokenService
    {
        private readonly JwtSettings _settings = jwtOptions.Value;

        public (string AccessToken, DateTime ExpiresAt) GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var expiresAt = now.AddMinutes(_settings.AccessTokenLifetimeMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: now,
                expires: expiresAt,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        public (string RawToken, string TokenHash, DateTime ExpiresAt) GenerateRefreshToken()
        {
            // 64 bytes of cryptographic randomness, URL-safe encoded.
            var rawToken = WebEncoders.Encode(RandomNumberGenerator.GetBytes(64));
            var expiresAt = timeProvider.GetUtcNow().UtcDateTime.AddDays(_settings.RefreshTokenLifetimeDays);

            return (rawToken, HashToken(rawToken), expiresAt);
        }

        /// <summary>SHA-256 hash — the database never stores raw refresh tokens.</summary>
        public string HashToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        private static class WebEncoders
        {
            public static string Encode(byte[] bytes) =>
                Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
