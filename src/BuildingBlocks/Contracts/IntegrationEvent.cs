namespace Contracts;

public abstract record IntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Bilinçli olarak init değil set: event oluşturulduktan sonra, yayınlanma/outbox anında
    /// EventBus tarafından o anki ambient correlation id ile damgalanır.
    /// </summary>
    public string? CorrelationId { get; set; }
}
