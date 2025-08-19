using System.ComponentModel.DataAnnotations;

namespace TaskManager.Web.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string JwtId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiryDate { get; set; }

        public bool IsUsed { get; set; } = false;

        public bool IsRevoked { get; set; } = false;

        public DateTime? RevokedAt { get; set; }

        public int UserId { get; set; }

        public virtual User User { get; set; } = null!;
    }
}
