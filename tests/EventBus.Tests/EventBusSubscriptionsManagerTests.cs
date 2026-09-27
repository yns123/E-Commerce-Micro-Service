using Contracts;
using EventBus;
using Xunit;

namespace EventBus.Tests;

public class EventBusSubscriptionsManagerTests
{
    [Fact]
    public void GetByRoutingKey_returns_empty_when_nothing_subscribed()
    {
        var manager = new EventBusSubscriptionsManager();

        var result = manager.GetByRoutingKey(UserRegisteredIntegrationEvent.RoutingKey);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByRoutingKey_returns_subscription_that_dispatches_to_the_registered_handler()
    {
        var manager = new EventBusSubscriptionsManager();
        var received = new List<IntegrationEvent>();

        manager.AddSubscription(
            typeof(UserRegisteredIntegrationEvent),
            UserRegisteredIntegrationEvent.RoutingKey,
            (_, @event, _) =>
            {
                received.Add(@event);
                return Task.CompletedTask;
            });

        var subscriptions = manager.GetByRoutingKey(UserRegisteredIntegrationEvent.RoutingKey).ToList();
        Assert.Single(subscriptions);

        var @event = new UserRegisteredIntegrationEvent(Guid.NewGuid(), "test@shop.local");
        await subscriptions[0].Dispatch(null!, @event, CancellationToken.None);

        Assert.Same(@event, Assert.Single(received));
    }

    [Fact]
    public void GetByRoutingKey_does_not_return_subscriptions_for_other_routing_keys()
    {
        var manager = new EventBusSubscriptionsManager();
        manager.AddSubscription(typeof(UserRegisteredIntegrationEvent), UserRegisteredIntegrationEvent.RoutingKey,
            (_, _, _) => Task.CompletedTask);

        var result = manager.GetByRoutingKey(OrderCreatedIntegrationEvent.RoutingKey);

        Assert.Empty(result);
    }
}
