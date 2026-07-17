namespace CleanArchitecture.Application.Dtos.Auth
{
    /// <summary>
    /// Внутренний результат: API-слой кладёт RefreshToken в cookie, а Response возвращает в теле.
    /// </summary>
    public class AuthResult
    {
        public AuthResponse Response { get; set; } = default!;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
