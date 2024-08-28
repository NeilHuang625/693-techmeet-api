using techmeet_api.Repositories;


namespace techmeet_api.Middlewares
{
    public class JwtBlacklistMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtBlacklistMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IJwtBlacklistService jwtBlacklistService)
        {
            string jwt = GetJwtFromRequest(context.Request);

            if (jwt != null && await jwtBlacklistService.IsTokenBlacklisted(jwt))
            {
                context.Response.StatusCode = 401; // Unauthorized
                return;
            }

            await _next(context);
        }

        private string GetJwtFromRequest(HttpRequest request)
        {
            string authorization = request.Headers["Authorization"];
            if (authorization != null && authorization.StartsWith("Bearer "))
            {
                return authorization.Substring("Bearer ".Length);
            }
            return null;
        }
    }
}