using System.ComponentModel.DataAnnotations;
using TaskManager.Web.Models;
using TaskStatus = TaskManager.Web.Models.TaskStatus;

namespace TaskManager.Web.DTOs
{
    public class TaskCreateDto
    {
        [Required(ErrorMessage = "Título é obrigatório")]
        [StringLength(200, ErrorMessage = "Título deve ter no máximo 200 caracteres")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Descrição deve ter no máximo 1000 caracteres")]
        public string Description { get; set; } = string.Empty;

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public DateTime? DueDate { get; set; }

        public int? AssignedToUserId { get; set; }

        public int? CategoryId { get; set; }
    }

    public class TaskUpdateDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Título é obrigatório")]
        [StringLength(200, ErrorMessage = "Título deve ter no máximo 200 caracteres")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Descrição deve ter no máximo 1000 caracteres")]
        public string Description { get; set; } = string.Empty;

        public TaskPriority Priority { get; set; }

        public TaskStatus Status { get; set; }

        public DateTime? DueDate { get; set; }

        public int? AssignedToUserId { get; set; }

        public int? CategoryId { get; set; }
    }

    public class TaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TaskPriority Priority { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string CreatedByUserName { get; set; } = string.Empty;
        public string? AssignedToUserName { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryColor { get; set; }

        public bool IsOverdue => DueDate.HasValue && DueDate < DateTime.Now && Status != TaskStatus.Completed;
        public bool IsDueSoon => DueDate.HasValue && DueDate <= DateTime.Now.AddDays(2) && Status != TaskStatus.Completed;
    }

    public class TaskFilterDto
    {
        public TaskStatus? Status { get; set; }
        public TaskPriority? Priority { get; set; }
        public int? CategoryId { get; set; }
        public int? AssignedToUserId { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;
    }

    public class TaskCommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int UserId { get; set; }
    }

    public class CreateTaskCommentDto
    {
        [Required(ErrorMessage = "Comentário é obrigatório")]
        [StringLength(1000, ErrorMessage = "Comentário deve ter no máximo 1000 caracteres")]
        public string Content { get; set; } = string.Empty;

        public int TaskId { get; set; }
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Color { get; set; } = "#007bff";
        public bool IsActive { get; set; }
        public int TaskCount { get; set; }
    }

    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Descrição deve ter no máximo 500 caracteres")]
        public string? Description { get; set; }

        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Cor deve estar no formato hexadecimal (#RRGGBB)")]
        public string Color { get; set; } = "#007bff";
    }

    public class TaskStatsDto
    {
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int DueSoonTasks { get; set; }
        public int HighPriorityTasks { get; set; }
        public int CriticalPriorityTasks { get; set; }
        public double CompletionRate { get; set; }
    }
}
