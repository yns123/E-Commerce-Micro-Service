using Contracts;

namespace EventBus;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IntegrationEvent;
}
