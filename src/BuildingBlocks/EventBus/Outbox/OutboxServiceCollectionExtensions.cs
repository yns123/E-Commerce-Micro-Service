using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventBus.Outbox;

public static class OutboxServiceCollectionExtensions
{
    public static IServiceCollection AddOutbox<TContext>(this IServiceCollection services) where TContext : DbContext
    {
        services.AddScoped<IOutbox, EfOutbox<TContext>>();
        services.AddHostedService<OutboxDispatcherHostedService<TContext>>();
        return services;
    }
}
