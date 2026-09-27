namespace Contracts;

public sealed record OrderCancelledIntegrationEvent(
    Guid OrderId,
    string UserEmail,
    string Reason) : IntegrationEvent
{
    public const string RoutingKey = "ordering.order.cancelled";
}
