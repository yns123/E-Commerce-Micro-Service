using System.Text.Json.Serialization;
using Common;
using Common.Correlation;
using Common.Logging;
using Contracts;
using EventBus;
using EventBus.Outbox;
using Microsoft.EntityFrameworkCore;
using Ordering.Api.Data;
using Ordering.Api.IntegrationEvents.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<OrderingDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure()));
builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddCorrelationId();
builder.Logging.AddStructuredLogging();
builder.Services.AddEventBus(builder.Configuration, queueName: "ordering.events")
    .AddSubscription<StockReservedIntegrationEvent, StockReservedHandler>()
    .AddSubscription<StockReservationFailedIntegrationEvent, StockReservationFailedHandler>();
builder.Services.AddOutbox<OrderingDbContext>();
builder.Services.AddHealthChecks().AddDbContextCheck<OrderingDbContext>();

var app = builder.Build();

await app.MigrateAsync();

app.UseCorrelationId();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/ordering/health");

app.Run();
