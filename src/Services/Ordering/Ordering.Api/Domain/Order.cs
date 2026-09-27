namespace Ordering.Api.Domain;

public sealed class Order
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string UserEmail { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public decimal Total { get; private set; }
    public string? CancelReason { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyList<OrderItem> Items => _items;

    private Order() { }

    public static Order Create(Guid userId, string userEmail, IEnumerable<(Guid ProductId, int Quantity)> items)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserEmail = userEmail,
            Status = OrderStatus.Pending,
            Total = 0,
            CreatedAt = DateTime.UtcNow,
        };

        foreach (var (productId, quantity) in items)
            order._items.Add(OrderItem.Create(order.Id, productId, quantity));

        return order;
    }

    public void Confirm(IReadOnlyDictionary<Guid, (string ProductName, decimal UnitPrice)> resolvedItems)
    {
        if (Status != OrderStatus.Pending) return;

        decimal total = 0;
        foreach (var item in _items)
        {
            if (!resolvedItems.TryGetValue(item.ProductId, out var info)) continue;
            item.SetDetails(info.ProductName, info.UnitPrice);
            total += info.UnitPrice * item.Quantity;
        }

        Total = total;
        Status = OrderStatus.Confirmed;
    }

    public void Cancel(string reason)
    {
        if (Status != OrderStatus.Pending) return;

        Status = OrderStatus.Cancelled;
        CancelReason = reason;
    }
}
