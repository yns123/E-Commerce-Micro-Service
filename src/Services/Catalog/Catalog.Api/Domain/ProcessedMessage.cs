namespace Catalog.Api.Domain;

public sealed class ProcessedMessage
{
    public Guid MessageId { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    private ProcessedMessage() { }

    public ProcessedMessage(Guid messageId)
    {
        MessageId = messageId;
        ProcessedAt = DateTime.UtcNow;
    }
}
