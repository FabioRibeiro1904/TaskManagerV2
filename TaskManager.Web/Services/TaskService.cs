using Microsoft.EntityFrameworkCore;
using TaskManager.Web.Data;
using TaskManager.Web.DTOs;
using TaskManager.Web.Models;
using TaskStatus = TaskManager.Web.Models.TaskStatus;

namespace TaskManager.Web.Services
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskDto>> GetTasksAsync(TaskFilterDto filter, int currentUserId, UserRole currentUserRole);
        Task<TaskDto?> GetTaskByIdAsync(int taskId, int currentUserId, UserRole currentUserRole);
        Task<TaskDto?> CreateTaskAsync(TaskCreateDto createDto, int currentUserId);
        Task<TaskDto?> UpdateTaskAsync(TaskUpdateDto updateDto, int currentUserId, UserRole currentUserRole);
        Task<bool> DeleteTaskAsync(int taskId, int currentUserId, UserRole currentUserRole);
        Task<bool> CompleteTaskAsync(int taskId, int currentUserId, UserRole currentUserRole);
        Task<TaskStatsDto> GetTaskStatsAsync(int currentUserId, UserRole currentUserRole);
        Task<IEnumerable<TaskCommentDto>> GetTaskCommentsAsync(int taskId, int currentUserId, UserRole currentUserRole);
        Task<TaskCommentDto?> AddTaskCommentAsync(CreateTaskCommentDto commentDto, int currentUserId);
        Task<bool> DeleteTaskCommentAsync(int commentId, int currentUserId, UserRole currentUserRole);
        Task<IEnumerable<CategoryDto>> GetCategoriesAsync();
        Task<CategoryDto?> CreateCategoryAsync(CreateCategoryDto createDto);
        Task<bool> DeleteCategoryAsync(int categoryId);
    }

    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TaskService> _logger;

        public TaskService(AppDbContext context, ILogger<TaskService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<TaskDto>> GetTasksAsync(TaskFilterDto filter, int currentUserId, UserRole currentUserRole)
        {
            try
            {
                var query = _context.Tasks
                    .Include(t => t.CreatedByUser)
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.Category)
                    .AsQueryable();

                if (currentUserRole == UserRole.User)
                {
                    query = query.Where(t => t.CreatedByUserId == currentUserId || t.AssignedToUserId == currentUserId);
                }

                if (filter.Status.HasValue)
                    query = query.Where(t => t.Status == filter.Status);

                if (filter.Priority.HasValue)
                    query = query.Where(t => t.Priority == filter.Priority);

                if (filter.CategoryId.HasValue)
                    query = query.Where(t => t.CategoryId == filter.CategoryId);

                if (filter.AssignedToUserId.HasValue)
                    query = query.Where(t => t.AssignedToUserId == filter.AssignedToUserId);

                if (filter.DueDateFrom.HasValue)
                    query = query.Where(t => t.DueDate >= filter.DueDateFrom);

                if (filter.DueDateTo.HasValue)
                    query = query.Where(t => t.DueDate <= filter.DueDateTo);

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    var searchTerm = filter.SearchTerm.ToLower();
                    query = query.Where(t => t.Title.ToLower().Contains(searchTerm) || 
                                           t.Description.ToLower().Contains(searchTerm));
                }

                query = filter.SortBy.ToLower() switch
                {
                    "title" => filter.SortDescending ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
                    "priority" => filter.SortDescending ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
                    "status" => filter.SortDescending ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
                    "duedate" => filter.SortDescending ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate),
                    _ => filter.SortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt)
                };

                var tasks = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(t => new TaskDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        Priority = t.Priority,
                        Status = t.Status,
                        DueDate = t.DueDate,
                        CreatedAt = t.CreatedAt,
                        CompletedAt = t.CompletedAt,
                        UpdatedAt = t.UpdatedAt,
                        CreatedByUserName = t.CreatedByUser.Name,
                        AssignedToUserName = t.AssignedToUser != null ? t.AssignedToUser.Name : null,
                        CategoryName = t.Category != null ? t.Category.Name : null,
                        CategoryColor = t.Category != null ? t.Category.Color : null
                    })
                    .ToListAsync();

                return tasks;
            }
            catch (Exception ex)
            {
                return new List<TaskDto>();
            }
        }

        public async Task<TaskDto?> GetTaskByIdAsync(int taskId, int currentUserId, UserRole currentUserRole)
        {
            try
            {
                var query = _context.Tasks
                    .Include(t => t.CreatedByUser)
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.Category)
                    .Where(t => t.Id == taskId);

                if (currentUserRole == UserRole.User)
                {
                    query = query.Where(t => t.CreatedByUserId == currentUserId || 
                                           (t.AssignedToUserId.HasValue && t.AssignedToUserId == currentUserId));
                }

                var task = await query
                    .Select(t => new TaskDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        Priority = t.Priority,
                        Status = t.Status,
                        DueDate = t.DueDate,
                        CreatedAt = t.CreatedAt,
                        CompletedAt = t.CompletedAt,
                        UpdatedAt = t.UpdatedAt,
                        CreatedByUserName = t.CreatedByUser.Name,
                        AssignedToUserName = t.AssignedToUser != null ? t.AssignedToUser.Name : null,
                        CategoryName = t.Category != null ? t.Category.Name : null,
                        CategoryColor = t.Category != null ? t.Category.Color : null
                    })
                    .FirstOrDefaultAsync();

                return task;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<TaskDto?> CreateTaskAsync(TaskCreateDto createDto, int currentUserId)
        {
            try
            {
                var task = new TaskItem
                {
                    Title = createDto.Title,
                    Description = createDto.Description,
                    Priority = createDto.Priority,
                    Status = TaskStatus.Pending,
                    DueDate = createDto.DueDate,
                    CreatedByUserId = currentUserId,
                    AssignedToUserId = createDto.AssignedToUserId,
                    CategoryId = createDto.CategoryId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();

                var createdTask = await _context.Tasks
                    .Include(t => t.CreatedByUser)
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.Category)
                    .Where(t => t.Id == task.Id)
                    .Select(t => new TaskDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        Priority = t.Priority,
                        Status = t.Status,
                        DueDate = t.DueDate,
                        CreatedAt = t.CreatedAt,
                        CompletedAt = t.CompletedAt,
                        UpdatedAt = t.UpdatedAt,
                        CreatedByUserName = t.CreatedByUser.Name,
                        AssignedToUserName = t.AssignedToUser != null ? t.AssignedToUser.Name : null,
                        CategoryName = t.Category != null ? t.Category.Name : null,
                        CategoryColor = t.Category != null ? t.Category.Color : null
                    })
                    .FirstOrDefaultAsync();

                return createdTask;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<TaskDto?> UpdateTaskAsync(TaskUpdateDto updateDto, int currentUserId, UserRole currentUserRole)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(updateDto.Id);
                if (task == null)
                    return null;

                if (currentUserRole == UserRole.User && 
                    task.CreatedByUserId != currentUserId && 
                    task.AssignedToUserId != currentUserId)
                {
                    return null;
                }

                task.Title = updateDto.Title;
                task.Description = updateDto.Description;
                task.Priority = updateDto.Priority;
                task.DueDate = updateDto.DueDate;
                task.AssignedToUserId = updateDto.AssignedToUserId;
                task.CategoryId = updateDto.CategoryId;
                task.UpdatedAt = DateTime.UtcNow;

                if (task.Status != TaskStatus.Completed && updateDto.Status == TaskStatus.Completed)
                {
                    task.CompletedAt = DateTime.UtcNow;
                }
                else if (task.Status == TaskStatus.Completed && updateDto.Status != TaskStatus.Completed)
                {
                    task.CompletedAt = null;
                }

                task.Status = updateDto.Status;

                await _context.SaveChangesAsync();

                return await GetTaskByIdAsync(task.Id, currentUserId, currentUserRole);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> DeleteTaskAsync(int taskId, int currentUserId, UserRole currentUserRole)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null)
                    return false;

                if (currentUserRole == UserRole.User && task.CreatedByUserId != currentUserId)
                {
                    return false;
                }

                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> CompleteTaskAsync(int taskId, int currentUserId, UserRole currentUserRole)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null)
                    return false;

                if (currentUserRole == UserRole.User && 
                    task.CreatedByUserId != currentUserId && 
                    task.AssignedToUserId != currentUserId)
                {
                    return false;
                }

                task.Status = TaskStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;
                task.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<TaskStatsDto> GetTaskStatsAsync(int currentUserId, UserRole currentUserRole)
        {
            try
            {
                var query = _context.Tasks.AsQueryable();

                if (currentUserRole == UserRole.User)
                {
                    query = query.Where(t => t.CreatedByUserId == currentUserId || t.AssignedToUserId == currentUserId);
                }

                var stats = new TaskStatsDto
                {
                    TotalTasks = await query.CountAsync(),
                    PendingTasks = await query.CountAsync(t => t.Status == TaskStatus.Pending),
                    InProgressTasks = await query.CountAsync(t => t.Status == TaskStatus.InProgress),
                    CompletedTasks = await query.CountAsync(t => t.Status == TaskStatus.Completed),
                    OverdueTasks = await query.CountAsync(t => t.DueDate.HasValue && t.DueDate < DateTime.Now && t.Status != TaskStatus.Completed),
                    DueSoonTasks = await query.CountAsync(t => t.DueDate.HasValue && t.DueDate <= DateTime.Now.AddDays(2) && t.Status != TaskStatus.Completed),
                    HighPriorityTasks = await query.CountAsync(t => t.Priority == TaskPriority.High && t.Status != TaskStatus.Completed),
                    CriticalPriorityTasks = await query.CountAsync(t => t.Priority == TaskPriority.Critical && t.Status != TaskStatus.Completed)
                };

                stats.CompletionRate = stats.TotalTasks > 0 ? (double)stats.CompletedTasks / stats.TotalTasks * 100 : 0;

                return stats;
            }
            catch (Exception ex)
            {
                return new TaskStatsDto();
            }
        }

        public async Task<IEnumerable<TaskCommentDto>> GetTaskCommentsAsync(int taskId, int currentUserId, UserRole currentUserRole)
        {
            try
            {
                var taskExists = await _context.Tasks
                    .Where(t => t.Id == taskId)
                    .Where(t => currentUserRole != UserRole.User || 
                               t.CreatedByUserId == currentUserId || 
                               t.AssignedToUserId == currentUserId)
                    .AnyAsync();

                if (!taskExists)
                    return new List<TaskCommentDto>();

                var comments = await _context.TaskComments
                    .Include(tc => tc.User)
                    .Where(tc => tc.TaskId == taskId)
                    .OrderBy(tc => tc.CreatedAt)
                    .Select(tc => new TaskCommentDto
                    {
                        Id = tc.Id,
                        Content = tc.Content,
                        CreatedAt = tc.CreatedAt,
                        UserName = tc.User.Name,
                        UserId = tc.UserId
                    })
                    .ToListAsync();

                return comments;
            }
            catch (Exception ex)
            {
                return new List<TaskCommentDto>();
            }
        }

        public async Task<TaskCommentDto?> AddTaskCommentAsync(CreateTaskCommentDto commentDto, int currentUserId)
        {
            try
            {
                var task = await _context.Tasks
                    .Where(t => t.Id == commentDto.TaskId)
                    .FirstOrDefaultAsync();

                if (task == null)
                    return null;

                var comment = new TaskComment
                {
                    Content = commentDto.Content,
                    TaskId = commentDto.TaskId,
                    UserId = currentUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TaskComments.Add(comment);
                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(currentUserId);

                return new TaskCommentDto
                {
                    Id = comment.Id,
                    Content = comment.Content,
                    CreatedAt = comment.CreatedAt,
                    UserName = user?.Name ?? "Usuário",
                    UserId = currentUserId
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> DeleteTaskCommentAsync(int commentId, int currentUserId, UserRole currentUserRole)
        {
            try
            {
                var comment = await _context.TaskComments.FindAsync(commentId);
                if (comment == null)
                    return false;

                if (currentUserRole == UserRole.User && comment.UserId != currentUserId)
                {
                    return false;
                }

                _context.TaskComments.Remove(comment);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        Color = c.Color,
                        IsActive = c.IsActive,
                        TaskCount = c.Tasks.Count
                    })
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return categories;
            }
            catch (Exception ex)
            {
                return new List<CategoryDto>();
            }
        }

        public async Task<CategoryDto?> CreateCategoryAsync(CreateCategoryDto createDto)
        {
            try
            {
                var category = new Category
                {
                    Name = createDto.Name,
                    Description = createDto.Description,
                    Color = createDto.Color,
                    IsActive = true
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description,
                    Color = category.Color,
                    IsActive = category.IsActive,
                    TaskCount = 0
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            try
            {
                var category = await _context.Categories.FindAsync(categoryId);
                if (category == null)
                    return false;

                category.IsActive = false;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
