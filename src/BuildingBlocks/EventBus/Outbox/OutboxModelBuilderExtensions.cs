using Microsoft.EntityFrameworkCore;

namespace EventBus.Outbox;

public static class OutboxModelBuilderExtensions
{
    public static void ConfigureOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Type).IsRequired();
            builder.Property(m => m.Content).IsRequired();
        });
    }
}
