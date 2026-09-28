using Common;
using Common.Correlation;
using Common.Logging;
using EventBus;
using Identity.Api.Auth;
using Identity.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<IdentityDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure()));
builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddCorrelationId();
builder.Logging.AddStructuredLogging();
builder.Services.AddEventBus(builder.Configuration, queueName: "identity.events");
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddHealthChecks().AddDbContextCheck<IdentityDbContext>();

var app = builder.Build();

await app.MigrateAndSeedAsync();

app.UseCorrelationId();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/identity/health");

app.Run();
