using Catalog.Api.Data;
using Catalog.Api.Domain;
using Contracts;
using EventBus;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.IntegrationEvents.Handlers;

public sealed class OrderCreatedHandler(CatalogDbContext db, IEventBus eventBus, ILogger<OrderCreatedHandler> logger)
    : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    private const int MaxConcurrencyRetries = 3;

    public async Task HandleAsync(OrderCreatedIntegrationEvent @event, CancellationToken ct)
    {
        if (await db.ProcessedMessages.AnyAsync(m => m.MessageId == @event.Id, ct))
            return;

        for (var attempt = 1; ; attempt++)
        {
            var productIds = @event.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

            var hasInsufficientStock = @event.Items.Any(i =>
                !products.TryGetValue(i.ProductId, out var product) || !product.HasSufficientStock(i.Quantity));

            if (hasInsufficientStock)
            {
                db.ProcessedMessages.Add(new ProcessedMessage(@event.Id));
                await db.SaveChangesAsync(ct);

                await eventBus.PublishAsync(new StockReservationFailedIntegrationEvent(@event.OrderId, "Yetersiz stok."), ct);
                return;
            }

            var reservedItems = @event.Items.Select(line =>
            {
                var product = products[line.ProductId];
                product.ReserveStock(line.Quantity);
                return new ReservedItem(product.Id, product.Name, product.Price, line.Quantity);
            }).ToList();

            db.ProcessedMessages.Add(new ProcessedMessage(@event.Id));

            try
            {
                await db.SaveChangesAsync(ct);
                await eventBus.PublishAsync(new StockReservedIntegrationEvent(@event.OrderId, reservedItems), ct);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
            {
                logger.LogWarning("Stok rezervasyonunda eşzamanlılık çakışması, {Attempt}. deneme. OrderId: {OrderId}", attempt, @event.OrderId);
                foreach (var entry in db.ChangeTracker.Entries().ToList())
                    entry.State = EntityState.Detached;
            }
        }
    }
}
