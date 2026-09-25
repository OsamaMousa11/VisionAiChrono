using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CleanArchitectureTemplate_Domain.Model.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;
using CleanArchitectureTemplate_Domain.Enumration;

namespace CleanArchitectureTemplate_infrastructure.Persistence
{
    public static class DbSeeder
    {
        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(DbSeeder).FullName ?? nameof(DbSeeder));

            var adminRole = RolesOption.ADMIN.ToString();

            // ── Roles from Domain Enumeration ──
            var roles = Enum.GetNames(typeof(RolesOption));

            foreach (var role in roles)
            {
                if (await roleManager.RoleExistsAsync(role))
                    continue;

                var result = await roleManager.CreateAsync(new ApplicationRole { Name = role });

                if (result.Succeeded)
                    logger.LogInformation("Seeded role {Role}.", role);
                else
                    logger.LogError("Failed to seed role {Role}: {Errors}", role,
                        string.Join(" | ", result.Errors.Select(e => e.Description)));
            }

            if (!await roleManager.RoleExistsAsync(adminRole))
                return;

            // ── Admin user from appsettings.json ──
            var adminSection = configuration.GetSection("AdminUser");

            var adminEmail = adminSection["Email"];
            var adminPassword = adminSection["Password"];
            var adminName = adminSection["Name"] ?? "Administrator";

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                logger.LogWarning("AdminUser:Email / AdminUser:Password not configured. Admin user was not seeded.");
                return;
            }

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser is null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail.Split('@')[0],
                    Email = adminEmail,
                    FullName = adminName,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    IsSuspended = false
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);

                if (!createResult.Succeeded)
                {
                    logger.LogError("Failed to create admin user {Email}: {Errors}", adminEmail,
                        string.Join(" | ", createResult.Errors.Select(e => e.Description)));
                    return;
                }

                logger.LogInformation("Created admin user {Email}.", adminEmail);
            }

            if (await userManager.IsInRoleAsync(adminUser, adminRole))
            {
                logger.LogInformation("Admin user {Email} already has the {Role} role.", adminEmail, adminRole);
                return;
            }

            var addToRole = await userManager.AddToRoleAsync(adminUser, adminRole);

            if (addToRole.Succeeded)
                logger.LogInformation("Assigned {Role} role to {Email}.", adminRole, adminEmail);
            else
                logger.LogError("Failed to assign {Role} to {Email}: {Errors}", adminRole, adminEmail,
                    string.Join(" | ", addToRole.Errors.Select(e => e.Description)));
        }
    }
}
