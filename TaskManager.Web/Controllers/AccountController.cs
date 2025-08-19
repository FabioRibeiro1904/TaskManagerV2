using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using TaskManager.Web.DTOs;
using TaskManager.Web.Services;

namespace TaskManager.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthService authService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var response = await _authService.LoginAsync(model);

                if (!response.Success)
                {
                    ModelState.AddModelError("", response.Message);
                    return View(model);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, response.User!.Id.ToString()),
                    new Claim(ClaimTypes.Name, response.User.Name),
                    new Claim(ClaimTypes.Email, response.User.Email),
                    new Claim(ClaimTypes.Role, response.User.Role),
                    new Claim("UserId", response.User.Id.ToString()),
                    new Claim("UserRole", response.User.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                TempData["SuccessMessage"] = $"Bem-vindo, {response.User.Name}!";
                return RedirectToAction("Dashboard", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante o login");
                ModelState.AddModelError("", "Erro interno do servidor. Tente novamente.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var response = await _authService.RegisterAsync(model);

                if (!response.Success)
                {
                    ModelState.AddModelError("", response.Message);
                    return View(model);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, response.User!.Id.ToString()),
                    new Claim(ClaimTypes.Name, response.User.Name),
                    new Claim(ClaimTypes.Email, response.User.Email),
                    new Claim(ClaimTypes.Role, response.User.Role),
                    new Claim("UserId", response.User.Id.ToString()),
                    new Claim("UserRole", response.User.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                TempData["SuccessMessage"] = $"Conta criada com sucesso! Bem-vindo, {response.User.Name}!";
                return RedirectToAction("Dashboard", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante o registro");
                ModelState.AddModelError("", "Erro interno do servidor. Tente novamente.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["InfoMessage"] = "Você foi desconectado com sucesso.";
            return RedirectToAction("Login");
        }
    }
}
