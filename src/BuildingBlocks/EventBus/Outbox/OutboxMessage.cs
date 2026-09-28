using System.Text.Json;
using Contracts;

namespace EventBus.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public DateTime OccurredAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage FromEvent<T>(T @event) where T : IntegrationEvent
    {
        return new OutboxMessage
        {
            Id = @event.Id,
            Type = typeof(T).AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(@event, EventBusJsonOptions.Default),
            OccurredAt = @event.OccurredAt,
        };
    }

    public void MarkProcessed() => ProcessedAt = DateTime.UtcNow;
}
