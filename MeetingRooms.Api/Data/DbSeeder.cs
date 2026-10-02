using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MeetingRooms.Api.Data;

/// <summary>Applies migrations and makes sure the Admin role and admin user exist.</summary>
public static class DbSeeder
{
    public const string AdminRole = "Admin";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync(AdminRole))
            await roles.CreateAsync(new IdentityRole(AdminRole));

        var email = config["Admin:Email"];
        var password = config["Admin:Password"];
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            return;

        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var admin = await users.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new IdentityUser { UserName = email, Email = email };
            var result = await users.CreateAsync(admin, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    "Admin seed failed: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        if (!await users.IsInRoleAsync(admin, AdminRole))
            await users.AddToRoleAsync(admin, AdminRole);
    }
}