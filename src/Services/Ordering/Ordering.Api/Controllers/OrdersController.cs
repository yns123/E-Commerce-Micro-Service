using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Common;
using Contracts;
using EventBus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ordering.Api.Data;
using Ordering.Api.Domain;
using Ordering.Api.Dtos;

namespace Ordering.Api.Controllers;

[ApiController]
[Route("api/ordering/orders")]
[Authorize]
public sealed class OrdersController(OrderingDbContext db, IEventBus eventBus) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateOrderResponse>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var userEmail = User.FindFirstValue(JwtRegisteredClaimNames.Email)!;

        var mergedItems = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => (ProductId: g.Key, Quantity: g.Sum(i => i.Quantity)))
            .ToList();

        var order = Order.Create(userId, userEmail, mergedItems);

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        await eventBus.PublishAsync(new OrderCreatedIntegrationEvent(
            order.Id,
            userId,
            userEmail,
            order.Items.Select(i => new OrderItemLine(i.ProductId, i.Quantity)).ToList()), ct);

        return Accepted(new CreateOrderResponse(order.Id, order.Status.ToString()));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderDto>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Orders.AsNoTracking().Include(o => o.Items).Where(o => o.UserId == userId);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new PagedResult<OrderDto>(items.Select(OrderDto.From).ToList(), page, pageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid id, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var order = await db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId, ct);

        return order is null ? NotFound() : OrderDto.From(order);
    }
}
