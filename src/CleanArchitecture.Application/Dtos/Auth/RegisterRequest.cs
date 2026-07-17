using System.ComponentModel.DataAnnotations;

namespace CleanArchitecture.Application.Dtos.Auth
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
}
