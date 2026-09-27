namespace Contracts;

public sealed record OrderConfirmedIntegrationEvent(
    Guid OrderId,
    string UserEmail,
    decimal Total) : IntegrationEvent
{
    public const string RoutingKey = "ordering.order.confirmed";
}
