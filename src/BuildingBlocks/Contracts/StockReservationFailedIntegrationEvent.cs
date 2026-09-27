namespace Contracts;

public sealed record StockReservationFailedIntegrationEvent(
    Guid OrderId,
    string Reason) : IntegrationEvent
{
    public const string RoutingKey = "catalog.stock.reservation-failed";
}
