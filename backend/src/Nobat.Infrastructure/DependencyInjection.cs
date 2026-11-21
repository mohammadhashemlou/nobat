using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nobat.Application.Common.Interfaces;
using Nobat.Domain.Entities;
using Nobat.Infrastructure.Common.Services;
using Nobat.Infrastructure.Data;
using Nobat.Infrastructure.Options;

namespace Nobat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<NobatDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(NobatDbContext).Assembly.FullName);
                });
        });

        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<NobatDbContext>()
            .AddDefaultTokenProviders();

        // JWT Options
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // Application services
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<NobatDbContext>());
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IDbConstraintDetector, DbConstraintDetector>();

        var timezoneId = configuration["BusinessTimezone"] ?? "Asia/Tehran";
        services.AddSingleton<IBusinessTimeProvider>(_ => new BusinessTimeProvider(timezoneId));

        return services;
    }
}
