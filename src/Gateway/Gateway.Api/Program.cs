using Common.Correlation;
using Common.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCorrelationId();
builder.Logging.AddStructuredLogging();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseCorrelationId();
app.MapReverseProxy();

app.Run();
