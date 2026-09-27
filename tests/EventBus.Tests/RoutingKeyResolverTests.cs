using Contracts;
using EventBus;
using Xunit;

namespace EventBus.Tests;

public class RoutingKeyResolverTests
{
    [Theory]
    [InlineData(typeof(UserRegisteredIntegrationEvent), "identity.user.registered")]
    [InlineData(typeof(OrderCreatedIntegrationEvent), "ordering.order.created")]
    [InlineData(typeof(StockReservedIntegrationEvent), "catalog.stock.reserved")]
    [InlineData(typeof(StockReservationFailedIntegrationEvent), "catalog.stock.reservation-failed")]
    [InlineData(typeof(OrderConfirmedIntegrationEvent), "ordering.order.confirmed")]
    [InlineData(typeof(OrderCancelledIntegrationEvent), "ordering.order.cancelled")]
    public void GetRoutingKey_returns_the_events_declared_routing_key(Type eventType, string expectedRoutingKey)
    {
        var routingKey = RoutingKeyResolver.GetRoutingKey(eventType);

        Assert.Equal(expectedRoutingKey, routingKey);
    }

    private sealed record EventWithoutRoutingKey : IntegrationEvent;

    [Fact]
    public void GetRoutingKey_throws_when_event_has_no_RoutingKey_constant()
    {
        Assert.Throws<InvalidOperationException>(() => RoutingKeyResolver.GetRoutingKey(typeof(EventWithoutRoutingKey)));
    }
}
