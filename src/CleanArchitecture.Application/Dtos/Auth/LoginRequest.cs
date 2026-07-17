using System.ComponentModel.DataAnnotations;

namespace CleanArchitecture.Application.Dtos.Auth
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress(ErrorMessage = "Некорректный email адрес.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
