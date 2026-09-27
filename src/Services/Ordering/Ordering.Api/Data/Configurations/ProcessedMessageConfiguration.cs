using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Api.Domain;

namespace Ordering.Api.Data.Configurations;

public sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.HasKey(m => m.MessageId);
    }
}
