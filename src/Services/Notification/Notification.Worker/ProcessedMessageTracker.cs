namespace Notification.Worker;

public sealed class ProcessedMessageTracker
{
    private const int MaxTracked = 1000;

    private readonly Queue<Guid> _order = new();
    private readonly HashSet<Guid> _seen = new();
    private readonly Lock _lock = new();

    public bool TryMarkProcessed(Guid messageId)
    {
        lock (_lock)
        {
            if (!_seen.Add(messageId))
                return false;

            _order.Enqueue(messageId);
            if (_order.Count > MaxTracked)
                _seen.Remove(_order.Dequeue());

            return true;
        }
    }
}
