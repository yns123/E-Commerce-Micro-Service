using System.Text.Json;
using Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventBus;

internal sealed class RabbitMqConsumerHostedService(
    RabbitMqConnectionManager connectionManager,
    EventBusSubscriptionsManager subscriptionsManager,
    EventBusQueueName queueName,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqConsumerHostedService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4)];

    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await connectionManager.GetConnectionAsync(stoppingToken);
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await EventBusTopology.DeclareExchangesAsync(_channel, stoppingToken);
        var routingKeys = subscriptionsManager.Subscriptions.Select(s => s.RoutingKey);
        await EventBusTopology.DeclareServiceQueueAsync(_channel, queueName.Value, routingKeys, stoppingToken);

        await _channel.BasicQosAsync(0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await _channel.BasicConsumeAsync(queueName.Value, autoAck: false, consumer, cancellationToken: stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // normal kapanış
        }
        finally
        {
            await _channel.DisposeAsync();
        }
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var routingKey = ea.RoutingKey;
        var subscriptions = subscriptionsManager.GetByRoutingKey(routingKey).ToList();

        if (subscriptions.Count == 0)
        {
            logger.LogWarning("Dinleyici bulunamayan routing key: {RoutingKey}, dead-letter'a gönderiliyor", routingKey);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        var eventType = subscriptions[0].EventType;
        IntegrationEvent @event;
        try
        {
            @event = (IntegrationEvent?)JsonSerializer.Deserialize(ea.Body.Span, eventType, JsonOptions)
                     ?? throw new JsonException("Mesaj gövdesi boş.");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Mesaj JSON olarak ayrıştırılamadı, dead-letter'a gönderiliyor. RoutingKey: {RoutingKey}", routingKey);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                foreach (var subscription in subscriptions)
                {
                    await subscription.Dispatch(scope.ServiceProvider, @event, CancellationToken.None);
                }

                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }
            catch (Exception ex) when (attempt < RetryDelays.Length)
            {
                logger.LogWarning(ex,
                    "Event işlenirken hata oluştu ({Attempt}. deneme), {Delay}s sonra tekrar denenecek. RoutingKey: {RoutingKey}",
                    attempt + 1, RetryDelays[attempt].TotalSeconds, routingKey);
                await Task.Delay(RetryDelays[attempt]);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Event {Attempts} denemeden sonra işlenemedi, dead-letter'a gönderiliyor. RoutingKey: {RoutingKey}",
                    RetryDelays.Length + 1, routingKey);
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }
        }
    }
}
