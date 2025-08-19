using System.ComponentModel.DataAnnotations;

namespace TaskManager.Web.Models
{
    public class TaskComment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Comentário é obrigatório")]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int TaskId { get; set; }
        public int UserId { get; set; }

        public virtual TaskItem Task { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
