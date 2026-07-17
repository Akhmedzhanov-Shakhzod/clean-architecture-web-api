using System.ComponentModel.DataAnnotations;
using CleanArchitecture.Application.Features.Users.Models;

namespace CleanArchitecture.Application.Features.Auth.Models
{
    public class RegisterRequest
    {
        [Required]
        [EmailAddress(ErrorMessage = "Некорректный email адрес.")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "Минимальная длина пароля — 8 символов.")]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress(ErrorMessage = "Некорректный email адрес.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest : IValidatableObject
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "Минимальная длина пароля — 8 символов.")]
        [MaxLength(128)]
        public string NewPassword { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (NewPassword == CurrentPassword)
                yield return new ValidationResult(
                    "Новый пароль должен отличаться от текущего.",
                    new[] { nameof(NewPassword) });
        }
    }

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
