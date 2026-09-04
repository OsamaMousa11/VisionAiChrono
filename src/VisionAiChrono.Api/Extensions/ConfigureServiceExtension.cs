using FluentValidation;
using Karaakeb.Core.DTO.AuthenticationDTO;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using VisionAiChrono.Application.Validators.AuthenticationValidator;
using CleanArchitectureTemplate_Api.Filters;
using CleanArchitectureTemplate_Api.Middlewares;
using CleanArchitectureTemplate_Application.ServiceContract;
using CleanArchitectureTemplate_Application.Services;
using CleanArchitectureTemplate_Domain.IRepositoryContract;
using CleanArchitectureTemplate_Domain.Model.Identity;
using CleanArchitectureTemplate_infrastructure.Data;
using CleanArchitectureTemplate_infrastructure.Repositories;
using System.Reflection;
using System.Text;


namespace VisionAiChrono.Api.Extensions;

public static class ConfigureServiceExtension
{
    public static IServiceCollection ServiceConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // ✅ Database
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("connstr") ??
                throw new InvalidOperationException("Connection string 'connstr' not found."));
        });

        // ✅ Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 5;
                options.Password.RequiredUniqueChars = 1;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddUserStore<UserStore<ApplicationUser, ApplicationRole, AppDbContext, Guid>>()
            .AddRoleStore<RoleStore<ApplicationRole, AppDbContext, Guid>>();

        // ✅ JWT + Cookie (for OAuth correlation) + Google
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "ExternalAuth";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.Cookie.SameSite = SameSiteMode.Lax;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = false;

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/hubs/notification"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = configuration["JWT:Issuer"],
                    ValidAudience = configuration["JWT:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["JWT:Key"] ?? "default_secret_key_for_development_only")),
                    ClockSkew = TimeSpan.Zero
                };
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = configuration["Authentication:Google:ClientId"]
                    ?? throw new InvalidOperationException("Google ClientId is missing.");
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"]
                    ?? throw new InvalidOperationException("Google ClientSecret is missing.");
                options.CallbackPath = configuration["Authentication:Google:CallbackPath"] ?? "/signin-google";
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.SaveTokens = true;
            });

        // ✅ CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        // ✅ Configuration
        services.Configure<JwtDTO>(configuration.GetSection("JWT"));
        services.Configure<MailSettings>(configuration.GetSection("MailSettings"));

        // ✅ Application Services
        services.AddTransient<IMailingService, MailingService>();
        services.AddScoped<IAuthenticationServices, AuthenticationServices>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddMemoryCache();
        services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();

        // ✅ Exception Handling
        services.AddExceptionHandling();

        // ✅ FluentValidation
        services.AddValidatorsFromAssembly(typeof(RegisterDTOValidator).Assembly);

        // ✅ Controllers & Filters
        services.AddControllers(options =>
            {
                options.Filters.Add<ValidationFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler =
                    System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

        services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        services.AddEndpointsApiExplorer();

        // ✅ Swagger Configuration
        services.AddSwaggerGen(config =>
        {
            config.EnableAnnotations();

            config.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "VisionAiChrono API",
                Version = "v1"
            });

            // Include XML Comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                config.IncludeXmlComments(xmlPath);

            // JWT Security Definition
            config.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token"
            });

            config.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", document),
                    new List<string>()
                }
            });
        });

        return services;
    }
}
