using CleanArchitectureTemplate_infrastructure.Persistence;
using Hangfire;
using VisionAiChrono.Api.Extensions;
using VisionAiChrono.Api.Filters;
using VisionAiChrono.Api.Hubs;
using VisionAiChrono.Api.Middlewares;
using VisionAiChrono.Application.VisionDetection;
using VisionAiChrono.Infrastructure.VisionDetection;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Project Service Configuration
builder.Services.ServiceConfiguration(builder.Configuration);

// SignalR
builder.Services.AddSignalR();

// Exception Middleware
builder.Services.AddTransient<ExceptionHandlingMiddleware>();

builder.Services.AddHttpClient<IVisionDetectionService, VisionDetectionService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8000");
    client.Timeout = TimeSpan.FromMinutes(3);
});
// Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration =
        builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379";
});

// Hangfire is registered inside ServiceConfiguration (ConfigureServiceExtension).

// Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

// Seed roles + the ADMIN user from appsettings (AdminUser section)
await using (var scope = app.Services.CreateAsyncScope())
{
    await DbSeeder.SeedAdminUserAsync(scope.ServiceProvider);
}

// Swagger
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "VisionAiChrono API v1"
    );

    options.RoutePrefix = "swagger";
});

// Exception Handling
app.UseExceptionHandling();

// Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
});

// Static Files
app.UseStaticFiles();

// CORS
app.UseCors("AllowAll");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

// SignalR
app.MapHub<NotificationHub>("/hubs/notifications");

// app.MapHub<MessageHub>("/hubs/messages");

app.Run();