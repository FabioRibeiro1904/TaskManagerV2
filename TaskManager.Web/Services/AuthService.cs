using Microsoft.EntityFrameworkCore;
using TaskManager.Web.Data;
using TaskManager.Web.DTOs;
using TaskManager.Web.Models;

namespace TaskManager.Web.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
        Task<bool> RevokeTokenAsync(string userId, string? jwtId = null);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> IsEmailTakenAsync(string email);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;

        public AuthService(
            AppDbContext context,
            IJwtService jwtService,
            ILogger<AuthService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.IsActive);

                if (user == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Email ou senha inválidos"
                    };
                }

                if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Email ou senha inválidos"
                    };
                }

                user.LastLoginAt = DateTime.UtcNow;

                var accessToken = _jwtService.GenerateJwtToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();
                var jwtId = _jwtService.GetJwtId(accessToken);

                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    JwtId = jwtId!,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddDays(
                        Convert.ToDouble(_configuration["JwtSettings:RefreshTokenExpirationDays"]))
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Login realizado com sucesso",
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    Expiration = DateTime.UtcNow.AddMinutes(
                        Convert.ToDouble(_configuration["JwtSettings:ExpirationMinutes"])),
                    User = new UserDto
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        Role = user.Role.ToString(),
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLoginAt
                    }
                };
            }
            catch (Exception ex)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Erro interno do servidor"
                };
            }
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                if (await IsEmailTakenAsync(registerDto.Email))
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Este email já está em uso"
                    };
                }

                var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

                var user = new User
                {
                    Name = registerDto.Name,
                    Email = registerDto.Email,
                    PasswordHash = passwordHash,
                    Role = UserRole.User,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var accessToken = _jwtService.GenerateJwtToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();
                var jwtId = _jwtService.GetJwtId(accessToken);

                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    JwtId = jwtId!,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddDays(
                        Convert.ToDouble(_configuration["JwtSettings:RefreshTokenExpirationDays"]))
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Usuário registrado com sucesso",
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    Expiration = DateTime.UtcNow.AddMinutes(
                        Convert.ToDouble(_configuration["JwtSettings:ExpirationMinutes"])),
                    User = new UserDto
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        Role = user.Role.ToString(),
                        CreatedAt = user.CreatedAt
                    }
                };
            }
            catch (Exception ex)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Erro interno do servidor"
                };
            }
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var principal = _jwtService.ValidateExpiredToken(refreshTokenDto.Token);
                if (principal == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Token inválido"
                    };
                }

                var jwtId = principal.Claims.FirstOrDefault(x => x.Type == "jti")?.Value;
                if (string.IsNullOrEmpty(jwtId))
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Token inválido"
                    };
                }

                var storedRefreshToken = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == refreshTokenDto.RefreshToken && 
                                              rt.JwtId == jwtId && 
                                              !rt.IsRevoked);

                if (storedRefreshToken == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Refresh token inválido"
                    };
                }

                if (storedRefreshToken.ExpiryDate < DateTime.UtcNow)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Refresh token expirado"
                    };
                }

                storedRefreshToken.IsRevoked = true;

                var newAccessToken = _jwtService.GenerateJwtToken(storedRefreshToken.User);
                var newRefreshToken = _jwtService.GenerateRefreshToken();
                var newJwtId = _jwtService.GetJwtId(newAccessToken);

                var newRefreshTokenEntity = new RefreshToken
                {
                    Token = newRefreshToken,
                    JwtId = newJwtId!,
                    UserId = storedRefreshToken.UserId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddDays(
                        Convert.ToDouble(_configuration["JwtSettings:RefreshTokenExpirationDays"]))
                };

                _context.RefreshTokens.Add(newRefreshTokenEntity);
                await _context.SaveChangesAsync();

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Token renovado com sucesso",
                    Token = newAccessToken,
                    RefreshToken = newRefreshToken,
                    Expiration = DateTime.UtcNow.AddMinutes(
                        Convert.ToDouble(_configuration["JwtSettings:ExpirationMinutes"])),
                    User = new UserDto
                    {
                        Id = storedRefreshToken.User.Id,
                        Name = storedRefreshToken.User.Name,
                        Email = storedRefreshToken.User.Email,
                        Role = storedRefreshToken.User.Role.ToString(),
                        CreatedAt = storedRefreshToken.User.CreatedAt,
                        LastLoginAt = storedRefreshToken.User.LastLoginAt
                    }
                };
            }
            catch (Exception ex)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Erro interno do servidor"
                };
            }
        }

        public async Task<bool> RevokeTokenAsync(string userId, string? jwtId = null)
        {
            try
            {
                var userIdInt = int.Parse(userId);
                var tokensQuery = _context.RefreshTokens.Where(rt => rt.UserId == userIdInt && !rt.IsRevoked);

                if (!string.IsNullOrEmpty(jwtId))
                {
                    tokensQuery = tokensQuery.Where(rt => rt.JwtId == jwtId);
                }

                var tokens = await tokensQuery.ToListAsync();

                foreach (var token in tokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
                {
                    return false;
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
                await _context.SaveChangesAsync();

                await RevokeTokenAsync(userId.ToString());

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        }

        public async Task<bool> IsEmailTakenAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
    }
}
