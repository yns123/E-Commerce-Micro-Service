using System.ComponentModel.DataAnnotations;
using Ordering.Api.Domain;

namespace Ordering.Api.Dtos;

public sealed record CreateOrderItemRequest(Guid ProductId, [Range(1, int.MaxValue)] int Quantity);

public sealed record CreateOrderRequest([Required, MinLength(1)] List<CreateOrderItemRequest> Items);

public sealed record CreateOrderResponse(Guid OrderId, string Status);

public sealed record OrderItemDto(Guid ProductId, string? ProductName, decimal? UnitPrice, int Quantity)
{
    public static OrderItemDto From(OrderItem item) => new(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);
}

public sealed record OrderDto(Guid Id, string Status, decimal Total, string? CancelReason, DateTime CreatedAt, IReadOnlyList<OrderItemDto> Items)
{
    public static OrderDto From(Order order) => new(
        order.Id,
        order.Status.ToString(),
        order.Total,
        order.CancelReason,
        order.CreatedAt,
        order.Items.Select(OrderItemDto.From).ToList());
}
