using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Web.DTOs;
using TaskManager.Web.Models;
using TaskManager.Web.Services;

namespace TaskManager.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TasksApiController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TasksApiController> _logger;

        public TasksApiController(ITaskService taskService, ILogger<TasksApiController> logger)
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
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasks([FromQuery] TaskFilterDto filter)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var tasks = await _taskService.GetTasksAsync(filter, currentUserId, currentUserRole);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDto>> GetTask(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var task = await _taskService.GetTaskByIdAsync(id, currentUserId, currentUserRole);

                if (task == null)
                {
                    return NotFound(new { message = "Tarefa não encontrada" });
                }

                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<TaskDto>> CreateTask([FromBody] TaskCreateDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUserId = GetCurrentUserId();

                if (currentUserId == 0)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var task = await _taskService.CreateTaskAsync(createDto, currentUserId);

                if (task == null)
                {
                    return BadRequest(new { message = "Erro ao criar tarefa" });
                }

                return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TaskDto>> UpdateTask(int id, [FromBody] TaskUpdateDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id != updateDto.Id)
                {
                    return BadRequest(new { message = "ID da URL não confere com o ID do corpo da requisição" });
                }

                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var task = await _taskService.UpdateTaskAsync(updateDto, currentUserId, currentUserRole);

                if (task == null)
                {
                    return NotFound(new { message = "Tarefa não encontrada ou sem permissão para editar" });
                }

                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTask(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var success = await _taskService.DeleteTaskAsync(id, currentUserId, currentUserRole);

                if (!success)
                {
                    return NotFound(new { message = "Tarefa não encontrada ou sem permissão para excluir" });
                }

                return Ok(new { message = "Tarefa excluída com sucesso" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpPost("{id}/complete")]
        public async Task<ActionResult> CompleteTask(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var success = await _taskService.CompleteTaskAsync(id, currentUserId, currentUserRole);

                if (!success)
                {
                    return NotFound(new { message = "Tarefa não encontrada ou sem permissão para completar" });
                }

                return Ok(new { message = "Tarefa marcada como concluída" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<TaskStatsDto>> GetTaskStats()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var stats = await _taskService.GetTaskStatsAsync(currentUserId, currentUserRole);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpGet("{id}/comments")]
        public async Task<ActionResult<IEnumerable<TaskCommentDto>>> GetTaskComments(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var comments = await _taskService.GetTaskCommentsAsync(id, currentUserId, currentUserRole);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpPost("{id}/comments")]
        public async Task<ActionResult<TaskCommentDto>> AddTaskComment(int id, [FromBody] CreateTaskCommentDto commentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUserId = GetCurrentUserId();

                if (currentUserId == 0)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                commentDto.TaskId = id;
                var comment = await _taskService.AddTaskCommentAsync(commentDto, currentUserId);

                if (comment == null)
                {
                    return BadRequest(new { message = "Erro ao adicionar comentário" });
                }

                return Ok(comment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpDelete("comments/{commentId}")]
        public async Task<ActionResult> DeleteTaskComment(int commentId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId == 0)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var success = await _taskService.DeleteTaskCommentAsync(commentId, currentUserId, currentUserRole);

                if (!success)
                {
                    return NotFound(new { message = "Comentário não encontrado ou sem permissão para excluir" });
                }

                return Ok(new { message = "Comentário excluído com sucesso" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }
    }
}
