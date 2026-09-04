namespace CleanArchitectureTemplate_Api.Middlewares
{
    public static class MiddlewareExtensions
    {
        public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
        {
            services.AddTransient<ExceptionHandlingMiddleware>();
            return services;
        }

        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            return app;
        }
    }
}