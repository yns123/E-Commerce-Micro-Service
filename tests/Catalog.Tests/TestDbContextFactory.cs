using Catalog.Api.Data;
using Catalog.Api.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Tests;

public static class TestDbContextFactory
{
    public static CatalogDbContext CreateInMemory(out SqliteConnection connection)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new CatalogDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    /// <summary>
    /// SQLite, SQL Server'ın aksine rowversion sütununu otomatik üretmez;
    /// concurrency token için testte elle bir başlangıç değeri veriyoruz.
    /// </summary>
    public static void AddWithRowVersion(this CatalogDbContext db, Product product)
    {
        db.Products.Add(product);
        db.Entry(product).Property(p => p.RowVersion).CurrentValue = Guid.NewGuid().ToByteArray();
    }
}
