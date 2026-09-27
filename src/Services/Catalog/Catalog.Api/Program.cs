using Catalog.Api.Data;
using Catalog.Api.IntegrationEvents.Handlers;
using Common;
using Contracts;
using EventBus;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<CatalogDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure()));
builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddEventBus(builder.Configuration, queueName: "catalog.events")
    .AddSubscription<OrderCreatedIntegrationEvent, OrderCreatedHandler>();
builder.Services.AddHealthChecks().AddDbContextCheck<CatalogDbContext>();

var app = builder.Build();

await app.MigrateAndSeedAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/catalog/health");

app.Run();
