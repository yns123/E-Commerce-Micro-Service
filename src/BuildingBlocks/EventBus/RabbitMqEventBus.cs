using System.Text.Json;
using Contracts;
using RabbitMQ.Client;

namespace EventBus;

internal sealed class RabbitMqEventBus(RabbitMqConnectionManager connectionManager) : IEventBus
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IntegrationEvent
    {
        var routingKey = RoutingKeyResolver.GetRoutingKey(typeof(T));

        var connection = await connectionManager.GetConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await EventBusTopology.DeclareExchangesAsync(channel, ct);

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, JsonOptions);
        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = @event.Id.ToString(),
            Type = routingKey,
        };

        await channel.BasicPublishAsync(
            exchange: EventBusTopology.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }
}
