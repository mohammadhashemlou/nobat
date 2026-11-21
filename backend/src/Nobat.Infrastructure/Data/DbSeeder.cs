using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nobat.Domain.Entities;
using Nobat.Domain.Enums;

namespace Nobat.Infrastructure.Data;

public static class DbSeeder
{
    public const string AdminPhoneNumber = "09120000000";
    public const string AdminPassword = "Admin@123456";

    public static async Task SeedAsync(
        NobatDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger? logger = null)
    {
        await context.Database.MigrateAsync();

        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        if (await userManager.FindByNameAsync(AdminPhoneNumber) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = AdminPhoneNumber,
                PhoneNumber = AdminPhoneNumber,
                PhoneNumberConfirmed = true,
                FullName = "مدیر سیستم",
                Role = UserRole.Admin,
                IsActive = true
            };

            var result = await userManager.CreateAsync(admin, AdminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, UserRole.Admin.ToString());
                logger?.LogInformation("Seeded admin account {PhoneNumber}", AdminPhoneNumber);
            }
            else
            {
                logger?.LogError("Could not seed the admin account: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        if (!await context.Salons.AnyAsync())
        {
            context.Salons.Add(new Salon
            {
                Id = Guid.NewGuid(),
                Name = "سالن نمونه",
                Slug = "default",
                Address = "تهران، خیابان ولیعصر",
                Phone = "02112345678",
                Description = "سالن زیبایی نمونه برای تست سیستم",
                DefaultTimezone = "Asia/Tehran"
            });

            await context.SaveChangesAsync();
        }
    }
}
