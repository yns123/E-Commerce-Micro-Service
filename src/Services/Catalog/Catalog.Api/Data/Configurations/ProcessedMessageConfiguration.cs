using Catalog.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Api.Data.Configurations;

public sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.HasKey(m => m.MessageId);
    }
}
