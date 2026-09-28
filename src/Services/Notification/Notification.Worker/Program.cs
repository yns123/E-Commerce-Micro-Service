using Contracts;
using EventBus;
using Notification.Worker;
using Notification.Worker.IntegrationEvents.Handlers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<ProcessedMessageTracker>();
builder.Services.AddEventBus(builder.Configuration, queueName: "notification.events")
    .AddSubscription<UserRegisteredIntegrationEvent, UserRegisteredHandler>()
    .AddSubscription<OrderConfirmedIntegrationEvent, OrderConfirmedHandler>()
    .AddSubscription<OrderCancelledIntegrationEvent, OrderCancelledHandler>();

var host = builder.Build();
host.Run();
