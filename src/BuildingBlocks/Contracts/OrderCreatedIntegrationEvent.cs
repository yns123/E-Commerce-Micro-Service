namespace Contracts;

public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    string UserEmail,
    IReadOnlyList<OrderItemLine> Items) : IntegrationEvent
{
    public const string RoutingKey = "ordering.order.created";
}

public sealed record OrderItemLine(Guid ProductId, int Quantity);
