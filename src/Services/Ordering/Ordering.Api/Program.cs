using EventBus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddEventBus(builder.Configuration, queueName: "ordering.events");

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapControllers();
app.MapHealthChecks("/api/ordering/health");

app.Run();
