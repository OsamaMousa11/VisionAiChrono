using VisionAiChrono.Api.Extensions;
using VisionAiChrono.Api.Hubs;
using VisionAiChrono.Api.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Project Service Configuration
builder.Services.ServiceConfiguration(builder.Configuration);

// SignalR
builder.Services.AddSignalR();

// Exception Middleware
builder.Services.AddTransient<ExceptionHandlingMiddleware>();

// Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration =
        builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379";
});

// Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

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