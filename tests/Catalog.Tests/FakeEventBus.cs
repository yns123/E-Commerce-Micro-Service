using Contracts;
using EventBus;

namespace Catalog.Tests;

public sealed class FakeEventBus : IEventBus
{
    public List<IntegrationEvent> Published { get; } = [];

    public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IntegrationEvent
    {
        Published.Add(@event);
        return Task.CompletedTask;
    }
}
