using System.ComponentModel.DataAnnotations;

namespace CleanArchitecture.Infrastructure.Helpers.Constants
{
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";

        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        /// <summary>HMAC-SHA256 signing key. Minimum 32 characters. Keep out of source control (user-secrets / env vars).</summary>
        [Required, MinLength(32)]
        public string Secret { get; set; } = string.Empty;

        [Range(1, 1440)]
        public int AccessTokenLifetimeMinutes { get; set; } = 15;

        [Range(1, 365)]
        public int RefreshTokenLifetimeDays { get; set; } = 7;
    }
}
