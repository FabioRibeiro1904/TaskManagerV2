using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Web.Data;
using TaskManager.Web.DTOs;
using TaskManager.Web.Models;

namespace TaskManager.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(AppDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private UserRole GetCurrentUserRole()
        {
            var roleClaim = User.FindFirst("UserRole")?.Value;
            return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : UserRole.User;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            try
            {
                var currentUserRole = GetCurrentUserRole();
                var currentUserId = GetCurrentUserId();

                if (currentUserId == 0)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var query = _context.Users.Where(u => u.IsActive);

                if (currentUserRole == UserRole.User)
                {
                    query = query.Where(u => u.Id == currentUserId);
                }

                var users = await query
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Email = u.Email,
                        Role = u.Role.ToString(),
                        CreatedAt = u.CreatedAt,
                        LastLoginAt = u.LastLoginAt
                    })
                    .OrderBy(u => u.Name)
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            try
            {
                var currentUserRole = GetCurrentUserRole();
                var currentUserId = GetCurrentUserId();

                if (currentUserId == 0)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                if (currentUserRole == UserRole.User && currentUserId != id)
                {
                    return Forbid();
                }

                var user = await _context.Users
                    .Where(u => u.Id == id && u.IsActive)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Email = u.Email,
                        Role = u.Role.ToString(),
                        CreatedAt = u.CreatedAt,
                        LastLoginAt = u.LastLoginAt
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { message = "Usuário não encontrado" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null || !user.IsActive)
                {
                    return NotFound(new { message = "Usuário não encontrado" });
                }

                var currentUserId = GetCurrentUserId();
                if (currentUserId == id)
                {
                    return BadRequest(new { message = "Não é possível alterar seu próprio role" });
                }

                user.Role = updateDto.Role;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Role do usuário atualizado com sucesso" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeactivateUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "Usuário não encontrado" });
                }

                var currentUserId = GetCurrentUserId();
                if (currentUserId == id)
                {
                    return BadRequest(new { message = "Não é possível desativar sua própria conta" });
                }

                user.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Usuário desativado com sucesso" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpPost("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ActivateUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "Usuário não encontrado" });
                }

                user.IsActive = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Usuário ativado com sucesso" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<object>> GetUserStats()
        {
            try
            {
                var stats = new
                {
                    TotalUsers = await _context.Users.CountAsync(u => u.IsActive),
                    TotalAdmins = await _context.Users.CountAsync(u => u.IsActive && u.Role == UserRole.Admin),
                    TotalManagers = await _context.Users.CountAsync(u => u.IsActive && u.Role == UserRole.Manager),
                    TotalRegularUsers = await _context.Users.CountAsync(u => u.IsActive && u.Role == UserRole.User),
                    UsersLastWeek = await _context.Users.CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-7)),
                    UsersLastMonth = await _context.Users.CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }
    }

    public class UpdateUserRoleDto
    {
        public UserRole Role { get; set; }
    }
}
