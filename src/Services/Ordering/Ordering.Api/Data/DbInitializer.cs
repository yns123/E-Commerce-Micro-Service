using Microsoft.EntityFrameworkCore;

namespace Ordering.Api.Data;

public static class DbInitializer
{
    public static async Task MigrateAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await db.Database.MigrateAsync();
    }
}
