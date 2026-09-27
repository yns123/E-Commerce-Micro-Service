using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Api.Domain;

namespace Ordering.Api.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.UserEmail).IsRequired().HasMaxLength(256);
        builder.Property(o => o.Total).HasPrecision(18, 2);
        builder.Property(o => o.CancelReason).HasMaxLength(500);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
