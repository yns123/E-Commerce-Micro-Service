using Contracts;

namespace EventBus;

public interface IIntegrationEventHandler<in T> where T : IntegrationEvent
{
    Task HandleAsync(T @event, CancellationToken ct);
}
