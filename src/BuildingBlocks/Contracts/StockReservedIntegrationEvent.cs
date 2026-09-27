namespace Contracts;

public sealed record StockReservedIntegrationEvent(
    Guid OrderId,
    IReadOnlyList<ReservedItem> Items) : IntegrationEvent
{
    public const string RoutingKey = "catalog.stock.reserved";
}

public sealed record ReservedItem(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
