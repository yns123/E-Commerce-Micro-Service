using Contracts;
using EventBus;

namespace Notification.Worker.IntegrationEvents.Handlers;

public sealed class UserRegisteredHandler(ProcessedMessageTracker tracker, ILogger<UserRegisteredHandler> logger)
    : IIntegrationEventHandler<UserRegisteredIntegrationEvent>
{
    public Task HandleAsync(UserRegisteredIntegrationEvent @event, CancellationToken ct)
    {
        if (!tracker.TryMarkProcessed(@event.Id))
            return Task.CompletedTask;

        logger.LogInformation("E-POSTA → {Email}: Hoş geldiniz!", @event.Email);
        return Task.CompletedTask;
    }
}
