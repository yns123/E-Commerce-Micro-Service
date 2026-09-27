using RabbitMQ.Client;

namespace EventBus;

internal static class EventBusTopology
{
    public const string ExchangeName = "ecommerce.events";
    public const string DeadLetterExchangeName = "ecommerce.events.dlx";
    public const string DeadLetterQueueName = "ecommerce.deadletter";

    public static async Task DeclareExchangesAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(DeadLetterExchangeName, ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: ct);
        await channel.QueueDeclareAsync(DeadLetterQueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(DeadLetterQueueName, DeadLetterExchangeName, routingKey: string.Empty, cancellationToken: ct);
    }

    public static async Task DeclareServiceQueueAsync(IChannel channel, string queueName, IEnumerable<string> routingKeys, CancellationToken ct)
    {
        var args = new Dictionary<string, object?> { ["x-dead-letter-exchange"] = DeadLetterExchangeName };
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: args, cancellationToken: ct);

        foreach (var routingKey in routingKeys.Distinct())
        {
            await channel.QueueBindAsync(queueName, ExchangeName, routingKey, cancellationToken: ct);
        }
    }
}
