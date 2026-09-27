using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ordering.Api.Data;

public sealed class OrderingDbContextFactory : IDesignTimeDbContextFactory<OrderingDbContext>
{
    public OrderingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Server=localhost,1433;Database=OrderingDb;User Id=sa;Password=Str0ng!Passw0rd;TrustServerCertificate=True;Encrypt=True";

        var optionsBuilder = new DbContextOptionsBuilder<OrderingDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new OrderingDbContext(optionsBuilder.Options);
    }
}
