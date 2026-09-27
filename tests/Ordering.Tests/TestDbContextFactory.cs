using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Ordering.Api.Data;

namespace Ordering.Tests;

public static class TestDbContextFactory
{
    public static OrderingDbContext CreateInMemory(out SqliteConnection connection)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new OrderingDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
