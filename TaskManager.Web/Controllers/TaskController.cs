using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Web.DTOs;
using TaskManager.Web.Models;
using TaskManager.Web.Services;

namespace TaskManager.Web.Controllers
{
    [Authorize]
    public class TaskController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TaskController> _logger;

        public TaskController(ITaskService taskService, ILogger<TaskController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        private UserRole GetCurrentUserRole()
        {
            var roleClaim = User.FindFirst("UserRole")?.Value;
            return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : UserRole.User;
        }

        [HttpGet]
        public async Task<IActionResult> Index(TaskFilterDto filter)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var tasks = await _taskService.GetTasksAsync(filter, currentUserId, currentUserRole);
                var categories = await _taskService.GetCategoriesAsync();
                
                ViewBag.Categories = categories;
                ViewBag.Filter = filter;
                ViewBag.CurrentUserRole = currentUserRole;
                
                return View(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar tarefas");
                TempData["ErrorMessage"] = "Erro ao carregar as tarefas.";
                return RedirectToAction("Dashboard", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var task = await _taskService.GetTaskByIdAsync(id, currentUserId, currentUserRole);

                if (task == null)
                {
                    TempData["ErrorMessage"] = "Tarefa não encontrada.";
                    return RedirectToAction("Index");
                }

                var comments = await _taskService.GetTaskCommentsAsync(id, currentUserId, currentUserRole);
                ViewBag.Comments = comments;

                return View(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar detalhes da tarefa {TaskId}", id);
                TempData["ErrorMessage"] = "Erro ao carregar os detalhes da tarefa.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var categories = await _taskService.GetCategoriesAsync();
            ViewBag.Categories = categories;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                var categories = await _taskService.GetCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }

            try
            {
                var currentUserId = GetCurrentUserId();

                if (currentUserId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var task = await _taskService.CreateTaskAsync(model, currentUserId);

                if (task == null)
                {
                    ModelState.AddModelError("", "Erro ao criar tarefa.");
                    var categories = await _taskService.GetCategoriesAsync();
                    ViewBag.Categories = categories;
                    return View(model);
                }

                TempData["SuccessMessage"] = "Tarefa criada com sucesso!";
                return RedirectToAction("Details", new { id = task.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar tarefa");
                ModelState.AddModelError("", "Erro interno do servidor.");
                var categories = await _taskService.GetCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var task = await _taskService.GetTaskByIdAsync(id, currentUserId, currentUserRole);

                if (task == null)
                {
                    TempData["ErrorMessage"] = "Tarefa não encontrada.";
                    return RedirectToAction("Index");
                }

                var categories = await _taskService.GetCategoriesAsync();
                ViewBag.Categories = categories;

                var updateDto = new TaskUpdateDto
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    Status = task.Status,
                    Priority = task.Priority,
                    DueDate = task.DueDate,
                    CategoryId = task.CategoryName != null ? 1 : null, // Você precisará ajustar isso conforme sua lógica
                    AssignedToUserId = task.AssignedToUserName != null ? 1 : null // Você precisará ajustar isso conforme sua lógica
                };

                return View(updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar tarefa para edição {TaskId}", id);
                TempData["ErrorMessage"] = "Erro ao carregar a tarefa.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskUpdateDto model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                var categories = await _taskService.GetCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var task = await _taskService.UpdateTaskAsync(model, currentUserId, currentUserRole);

                if (task == null)
                {
                    TempData["ErrorMessage"] = "Tarefa não encontrada ou sem permissão para editar.";
                    return RedirectToAction("Index");
                }

                TempData["SuccessMessage"] = "Tarefa atualizada com sucesso!";
                return RedirectToAction("Details", new { id = task.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar tarefa {TaskId}", id);
                ModelState.AddModelError("", "Erro interno do servidor.");
                var categories = await _taskService.GetCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var success = await _taskService.DeleteTaskAsync(id, currentUserId, currentUserRole);

                if (!success)
                {
                    TempData["ErrorMessage"] = "Tarefa não encontrada ou sem permissão para excluir.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Tarefa excluída com sucesso!";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir tarefa {TaskId}", id);
                TempData["ErrorMessage"] = "Erro ao excluir a tarefa.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var success = await _taskService.CompleteTaskAsync(id, currentUserId, currentUserRole);

                if (!success)
                {
                    TempData["ErrorMessage"] = "Tarefa não encontrada ou sem permissão para completar.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Tarefa marcada como concluída!";
                }

                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao completar tarefa {TaskId}", id);
                TempData["ErrorMessage"] = "Erro ao completar a tarefa.";
                return RedirectToAction("Details", new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int taskId, CreateTaskCommentDto model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Comentário inválido.";
                return RedirectToAction("Details", new { id = taskId });
            }

            try
            {
                var currentUserId = GetCurrentUserId();

                if (currentUserId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                model.TaskId = taskId;
                var comment = await _taskService.AddTaskCommentAsync(model, currentUserId);

                if (comment == null)
                {
                    TempData["ErrorMessage"] = "Erro ao adicionar comentário.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Comentário adicionado com sucesso!";
                }

                return RedirectToAction("Details", new { id = taskId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar comentário na tarefa {TaskId}", taskId);
                TempData["ErrorMessage"] = "Erro ao adicionar comentário.";
                return RedirectToAction("Details", new { id = taskId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsCompleted(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var success = await _taskService.CompleteTaskAsync(id, currentUserId, currentUserRole);

                if (success)
                {
                    TempData["SuccessMessage"] = "Tarefa marcada como concluída com sucesso!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao marcar tarefa como concluída. Verifique se você tem permissão para esta ação.";
                }

                return RedirectToAction("Details", new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao marcar tarefa {TaskId} como concluída", id);
                TempData["ErrorMessage"] = "Erro interno do servidor ao marcar tarefa como concluída.";
                return RedirectToAction("Details", new { id = id });
            }
        }
    }
}
