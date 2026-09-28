using System.Text.Json;
using Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventBus.Outbox;

internal sealed class OutboxDispatcherHostedService<TContext>(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxDispatcherHostedService<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox mesajları gönderilirken hata oluştu");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // normal kapanış
            }
        }
    }

    private async Task DispatchPendingAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var pending = await db.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(20)
            .ToListAsync(ct);

        foreach (var message in pending)
        {
            var eventType = Type.GetType(message.Type)
                ?? throw new InvalidOperationException($"Outbox mesajı tipi bulunamadı: {message.Type}");

            var @event = (IntegrationEvent)JsonSerializer.Deserialize(message.Content, eventType, EventBusJsonOptions.Default)!;

            var publishMethod = typeof(IEventBus).GetMethod(nameof(IEventBus.PublishAsync))!.MakeGenericMethod(eventType);
            await (Task)publishMethod.Invoke(eventBus, [@event, ct])!;

            message.MarkProcessed();
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
