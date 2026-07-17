using System.ComponentModel.DataAnnotations;

namespace CleanArchitecture.Application.Dtos.Auth
{
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
}
