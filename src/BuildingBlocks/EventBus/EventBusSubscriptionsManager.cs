using Contracts;

namespace EventBus;

public sealed record Subscription(Type EventType, string RoutingKey, Func<IServiceProvider, IntegrationEvent, CancellationToken, Task> Dispatch);

public sealed class EventBusSubscriptionsManager
{
    private readonly List<Subscription> _subscriptions = [];

    public IReadOnlyList<Subscription> Subscriptions => _subscriptions;

    public void AddSubscription(Type eventType, string routingKey, Func<IServiceProvider, IntegrationEvent, CancellationToken, Task> dispatch)
        => _subscriptions.Add(new Subscription(eventType, routingKey, dispatch));

    public IEnumerable<Subscription> GetByRoutingKey(string routingKey)
        => _subscriptions.Where(s => s.RoutingKey == routingKey);
}
