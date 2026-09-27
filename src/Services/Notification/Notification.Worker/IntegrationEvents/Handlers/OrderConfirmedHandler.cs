using Contracts;
using EventBus;

namespace Notification.Worker.IntegrationEvents.Handlers;

public sealed class OrderConfirmedHandler(ProcessedMessageTracker tracker, ILogger<OrderConfirmedHandler> logger)
    : IIntegrationEventHandler<OrderConfirmedIntegrationEvent>
{
    public Task HandleAsync(OrderConfirmedIntegrationEvent @event, CancellationToken ct)
    {
        if (!tracker.TryMarkProcessed(@event.Id))
            return Task.CompletedTask;

        logger.LogInformation("E-POSTA → {Email}: Siparişiniz onaylandı! Toplam: {Total}", @event.UserEmail, @event.Total);
        return Task.CompletedTask;
    }
}
