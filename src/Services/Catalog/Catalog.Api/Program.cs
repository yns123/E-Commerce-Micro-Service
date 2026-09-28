using Catalog.Api.Data;
using Catalog.Api.IntegrationEvents.Handlers;
using Common;
using Common.Correlation;
using Common.Logging;
using Contracts;
using EventBus;
using EventBus.Outbox;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<CatalogDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure()));
builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddCorrelationId();
builder.Logging.AddStructuredLogging();
builder.Services.AddEventBus(builder.Configuration, queueName: "catalog.events")
    .AddSubscription<OrderCreatedIntegrationEvent, OrderCreatedHandler>();
builder.Services.AddOutbox<CatalogDbContext>();
builder.Services.AddHealthChecks().AddDbContextCheck<CatalogDbContext>();

var app = builder.Build();

await app.MigrateAndSeedAsync();

app.UseCorrelationId();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/catalog/health");

app.Run();
