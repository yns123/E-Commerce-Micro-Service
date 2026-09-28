using Common.Correlation;
using Contracts;

namespace EventBus;

internal static class CorrelationStamper
{
    /// <summary>
    /// Ambient bir correlation id varsa event'e yazar. Yoksa (ör. OutboxDispatcher'ın kendi
    /// arka plan döngüsü) event'in zaten taşıdığı değeri olduğu gibi bırakır.
    /// </summary>
    public static void Stamp(IntegrationEvent @event, ICorrelationIdAccessor accessor)
    {
        if (!string.IsNullOrEmpty(accessor.CorrelationId))
            @event.CorrelationId = accessor.CorrelationId;
    }
}
