using Identity.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Data;

public static class DbInitializer
{
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync())
        {
            var hasher = new PasswordHasher<User>();
            var admin = User.Register("admin@shop.local", Roles.Admin);
            admin.SetPasswordHash(hasher.HashPassword(admin, "Admin123!"));

            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}
