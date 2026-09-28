using Contracts;

namespace EventBus.Outbox;

public interface IOutbox
{
    /// <summary>
    /// Event'i o anki DbContext'in change tracker'ına ekler; SaveChangesAsync henüz çağrılmaz.
    /// Çağıran kod, iş değişikliğiyle birlikte tek bir SaveChangesAsync ile ikisini de kaydeder.
    /// </summary>
    void Enqueue<T>(T @event) where T : IntegrationEvent;
}
