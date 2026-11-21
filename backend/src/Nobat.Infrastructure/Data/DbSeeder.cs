using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nobat.Domain.Entities;
using Nobat.Domain.Enums;

namespace Nobat.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        NobatDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger? logger = null)
    {
        await context.Database.MigrateAsync();

        // Seed roles
        var roles = Enum.GetNames<UserRole>();
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                logger?.LogInformation("Seeded role: {Role}", role);
            }
        }

        // Seed default admin user
        var adminEmail = "admin@nobat.local";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "مدیر سیستم",
                Role = UserRole.Admin,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, UserRole.Admin.ToString());
                logger?.LogInformation("Seeded admin user: {Email}", adminEmail);
            }
            else
            {
                logger?.LogError("Failed to seed admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        // Seed a default salon
        if (!await context.Salons.AnyAsync())
        {
            var salon = new Salon
            {
                Id = Guid.NewGuid(),
                Name = "سالن نمونه",
                Slug = "default",
                Address = "تهران، خیابان ولیعصر",
                Phone = "02112345678",
                Description = "سالن زیبایی نمونه برای تست سیستم",
                DefaultTimezone = "Asia/Tehran"
            };

            context.Salons.Add(salon);
            await context.SaveChangesAsync();
            logger?.LogInformation("Seeded default salon: {Name}", salon.Name);
        }

        await context.SaveChangesAsync();
    }
}
