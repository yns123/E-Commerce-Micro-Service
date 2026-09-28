using Common.Correlation;
using Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EventBus;

public sealed class EventBusBuilder(IServiceCollection services, EventBusSubscriptionsManager subscriptionsManager)
{
    public EventBusBuilder AddSubscription<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        services.AddScoped<THandler>();

        var routingKey = RoutingKeyResolver.GetRoutingKey(typeof(TEvent));
        subscriptionsManager.AddSubscription(typeof(TEvent), routingKey, async (sp, @event, ct) =>
        {
            var handler = sp.GetRequiredService<THandler>();
            await handler.HandleAsync((TEvent)@event, ct);
        });

        return this;
    }
}

public static class ServiceCollectionExtensions
{
    public static EventBusBuilder AddEventBus(this IServiceCollection services, IConfiguration configuration, string queueName)
    {
        services.TryAddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();
        services.Configure<EventBusOptions>(configuration.GetSection("RabbitMq"));
        services.AddSingleton(new EventBusQueueName(queueName));
        services.AddSingleton<RabbitMqConnectionManager>();
        services.AddSingleton<IEventBus, RabbitMqEventBus>();
        services.AddHostedService<RabbitMqConsumerHostedService>();

        var subscriptionsManager = new EventBusSubscriptionsManager();
        services.AddSingleton(subscriptionsManager);

        return new EventBusBuilder(services, subscriptionsManager);
    }
}
