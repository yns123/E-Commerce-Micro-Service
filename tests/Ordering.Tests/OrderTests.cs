using Ordering.Api.Domain;
using Xunit;

namespace Ordering.Tests;

public class OrderTests
{
    [Fact]
    public void Confirm_sets_status_to_Confirmed_and_calculates_total()
    {
        var order = Order.Create(Guid.NewGuid(), "user@example.com", [(Guid.NewGuid(), 2)]);
        var productId = order.Items[0].ProductId;

        order.Confirm(new Dictionary<Guid, (string, decimal)> { [productId] = ("Ürün", 25m) });

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(50m, order.Total);
        Assert.Equal("Ürün", order.Items[0].ProductName);
        Assert.Equal(25m, order.Items[0].UnitPrice);
    }

    [Fact]
    public void Confirm_is_a_no_op_when_order_already_confirmed()
    {
        var order = Order.Create(Guid.NewGuid(), "user@example.com", [(Guid.NewGuid(), 1)]);
        var productId = order.Items[0].ProductId;
        order.Confirm(new Dictionary<Guid, (string, decimal)> { [productId] = ("Ürün", 10m) });

        order.Confirm(new Dictionary<Guid, (string, decimal)> { [productId] = ("Farklı Ürün", 999m) });

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(10m, order.Total);
        Assert.Equal("Ürün", order.Items[0].ProductName);
    }

    [Fact]
    public void Cancel_sets_status_to_Cancelled_and_reason()
    {
        var order = Order.Create(Guid.NewGuid(), "user@example.com", [(Guid.NewGuid(), 1)]);

        order.Cancel("Yetersiz stok.");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Yetersiz stok.", order.CancelReason);
    }

    [Fact]
    public void Cancel_is_a_no_op_when_order_already_confirmed()
    {
        var order = Order.Create(Guid.NewGuid(), "user@example.com", [(Guid.NewGuid(), 1)]);
        var productId = order.Items[0].ProductId;
        order.Confirm(new Dictionary<Guid, (string, decimal)> { [productId] = ("Ürün", 10m) });

        order.Cancel("Çok geç");

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Null(order.CancelReason);
    }
}
