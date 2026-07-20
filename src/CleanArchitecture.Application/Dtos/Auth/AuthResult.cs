using CleanArchitecture.Application.Dtos.Users;

namespace CleanArchitecture.Application.Dtos.Auth
{
    /// <summary>
    /// Внутренний результат: API-слой кладёт токены в HttpOnly cookies, а клиенту в теле возвращается только User.
    /// </summary>
    public class AuthResult
    {
        public UserDto User { get; set; } = default!;
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
