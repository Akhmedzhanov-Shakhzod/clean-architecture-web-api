using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Infrastructure.Helpers.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Infrastructure.Helpers.DbContexts
{
    public static class DbInitializer
    {
        /// <summary>Applies pending migrations and seeds roles + default admin.</summary>
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied.");

            await SeedRolesAsync(services);
            await SeedAdminAsync(services, logger);
        }

        private static async Task SeedRolesAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            foreach (var role in Roles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        private static async Task SeedAdminAsync(IServiceProvider services, ILogger logger)
        {
            var settings = services.GetRequiredService<IOptions<AdminSeedSettings>>().Value;

            if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password))
            {
                logger.LogWarning("AdminSeed settings are empty — skipping admin seeding.");
                return;
            }

            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            if (await userManager.FindByEmailAsync(settings.Email) is not null)
                return;

            var admin = new ApplicationUser
            {
                UserName = settings.Email,
                Email = settings.Email,
                FirstName = settings.FirstName,
                LastName = settings.LastName,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, settings.Password);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to seed admin user: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(admin, Roles.Admin);
            logger.LogInformation("Admin user {Email} seeded.", settings.Email);
        }
    }
}
