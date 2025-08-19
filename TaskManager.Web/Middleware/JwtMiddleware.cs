using TaskManager.Web.Services;

namespace TaskManager.Web.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtMiddleware> _logger;

        public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
        {
            var token = context.Request.Headers["Authorization"]
                .FirstOrDefault()?.Split(" ").Last();

            if (token != null)
            {
                try
                {
                    var principal = jwtService.ValidateJwtToken(token);
                    if (principal != null)
                    {
                        context.User = principal;
                    }
                }
                catch (Exception ex)
                {
                }
            }

            await _next(context);
        }
    }
}
