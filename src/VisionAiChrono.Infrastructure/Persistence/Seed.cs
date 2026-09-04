using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
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

            // ── Roles from Domain Enumeration ──
            var roles = Enum.GetNames(typeof(RolesOption));

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = role,
                        NormalizedName = role.ToUpperInvariant()
                    });
                }
            }

            // ── Admin user from appsettings.json ──
            var adminSection = configuration.GetSection("AdminUser");

            string adminEmail = adminSection["Email"]
                ?? throw new InvalidOperationException(
                    "Missing 'AdminUser:Email' in appsettings.json.");

            string adminPassword = adminSection["Password"]
                ?? throw new InvalidOperationException(
                    "Missing 'AdminUser:Password' in appsettings.json.");

            string adminName = adminSection["Name"]
                ?? throw new InvalidOperationException(
                    "Missing 'AdminUser:Name' in appsettings.json.");

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
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
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, CleanArchitectureTemplate_Domain.Enumration.RolesOption.ADMIN.ToString());
                }
                else
                {
                    var errors = string.Join(" | ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create admin user: {errors}");
                }
            }
        }
    }
}
