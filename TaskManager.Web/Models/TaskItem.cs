using System.ComponentModel.DataAnnotations;

namespace TaskManager.Web.Models
{
    public enum TaskPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum TaskStatus
    {
        Pending = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }

    public class TaskItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Título é obrigatório")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public TaskStatus Status { get; set; } = TaskStatus.Pending;

        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int CreatedByUserId { get; set; }
        public int? AssignedToUserId { get; set; }
        public int? CategoryId { get; set; }

        public virtual User CreatedByUser { get; set; } = null!;
        public virtual User? AssignedToUser { get; set; }
        public virtual Category? Category { get; set; }
        public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    }
}
