using CleanArchitecture.Domain.Models;

namespace CleanArchitecture.Domain.Entities
{
    /// <summary>
    /// Refresh token. Only the SHA-256 hash is stored — never the raw value.
    /// Rotation chain is tracked via <see cref="ReplacedByTokenHash"/> to detect token reuse.
    /// </summary>
    public class RefreshToken : BaseEntity
    {
        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = default!;

        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedByIp { get; set; }

        public DateTime? RevokedAt { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByTokenHash { get; set; }
        public string? ReasonRevoked { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt is not null;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
