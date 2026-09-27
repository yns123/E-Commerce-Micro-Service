using EventBus;
using Notification.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddEventBus(builder.Configuration, queueName: "notification.events");

var host = builder.Build();
host.Run();
