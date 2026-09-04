using CleanArchitectureTemplate_Application.ServiceContract;

namespace CleanArchitectureTemplate_Api.Middlewares
{
    public class TokenBlacklistMiddleware : IMiddleware
    {
        private readonly ITokenBlacklistService _blacklistService;

        public TokenBlacklistMiddleware(ITokenBlacklistService blacklistService)
        {
            _blacklistService = blacklistService;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var authHeader = context.Request.Headers["Authorization"].ToString();
            var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader[7..]
                : null;

            if (!string.IsNullOrEmpty(token) && _blacklistService.IsBlacklisted(token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";

                var problemDetails = new
                {
                    status = 401,
                    title = "Unauthorized",
                    detail = "انتهت صلاحية الجلسة. يرجى تسجيل الدخول مجدداً."
                };

                await context.Response.WriteAsJsonAsync(problemDetails);
                return;
            }

            await next(context);
        }
    }
}
