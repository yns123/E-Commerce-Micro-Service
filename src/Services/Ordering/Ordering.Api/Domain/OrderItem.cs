namespace Ordering.Api.Domain;

public sealed class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string? ProductName { get; private set; }
    public decimal? UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    private OrderItem() { }

    internal static OrderItem Create(Guid orderId, Guid productId, int quantity)
    {
        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            Quantity = quantity,
        };
    }

    internal void SetDetails(string productName, decimal unitPrice)
    {
        ProductName = productName;
        UnitPrice = unitPrice;
    }
}
