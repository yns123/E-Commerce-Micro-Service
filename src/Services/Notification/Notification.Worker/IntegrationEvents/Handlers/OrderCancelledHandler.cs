using Contracts;
using EventBus;

namespace Notification.Worker.IntegrationEvents.Handlers;

public sealed class OrderCancelledHandler(ProcessedMessageTracker tracker, ILogger<OrderCancelledHandler> logger)
    : IIntegrationEventHandler<OrderCancelledIntegrationEvent>
{
    public Task HandleAsync(OrderCancelledIntegrationEvent @event, CancellationToken ct)
    {
        if (!tracker.TryMarkProcessed(@event.Id))
            return Task.CompletedTask;

        logger.LogInformation("E-POSTA → {Email}: Siparişiniz iptal edildi. Sebep: {Reason}", @event.UserEmail, @event.Reason);
        return Task.CompletedTask;
    }
}
