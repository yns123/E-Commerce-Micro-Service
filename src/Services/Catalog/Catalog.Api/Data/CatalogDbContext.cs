using Catalog.Api.Domain;
using EventBus.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        modelBuilder.ConfigureOutbox();

        if (!Database.IsSqlServer())
        {
            // SQLite (birim testlerinde kullanılır) SQL Server'ın rowversion'ı gibi otomatik
            // değer üretmiyor; concurrency token olarak kalsın ama testte elle atanabilsin.
            modelBuilder.Entity<Product>().Property(p => p.RowVersion).ValueGeneratedNever();
        }
    }
}
