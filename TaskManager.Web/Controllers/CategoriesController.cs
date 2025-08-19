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
    public class CategoriesController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ITaskService taskService, ILogger<CategoriesController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        private UserRole GetCurrentUserRole()
        {
            var roleClaim = User.FindFirst("UserRole")?.Value;
            return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : UserRole.User;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            try
            {
                var categories = await _taskService.GetCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var category = await _taskService.CreateCategoryAsync(createDto);

                if (category == null)
                {
                    return BadRequest(new { message = "Erro ao criar categoria" });
                }

                return CreatedAtAction(nameof(GetCategories), null, category);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            try
            {
                var success = await _taskService.DeleteCategoryAsync(id);

                if (!success)
                {
                    return NotFound(new { message = "Categoria não encontrada" });
                }

                return Ok(new { message = "Categoria excluída com sucesso" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }
    }
}
