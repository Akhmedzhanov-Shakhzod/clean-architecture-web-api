using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Infrastructure.Identity
{
    public interface ITokenService
    {
        (string AccessToken, DateTime ExpiresAt) GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles);
        (string RawToken, string TokenHash, DateTime ExpiresAt) GenerateRefreshToken();
        string HashToken(string token);
    }
}
