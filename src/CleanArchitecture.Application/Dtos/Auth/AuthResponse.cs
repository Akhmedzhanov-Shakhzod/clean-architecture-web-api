using CleanArchitecture.Application.Dtos.Users;

namespace CleanArchitecture.Application.Dtos.Auth
{
    /// <summary>
    /// Тело ответа клиенту. Refresh-токен сюда не попадает — он передаётся только в HttpOnly cookie.
    /// </summary>
    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public DateTime AccessTokenExpiresAt { get; set; }
        public UserDto User { get; set; } = default!;
    }
}
