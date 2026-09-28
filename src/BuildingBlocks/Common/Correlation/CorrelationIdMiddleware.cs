using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Common.Correlation;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor accessor, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        // Gateway'den geliyorsa zaten var; yoksa burada üretilen değer downstream'e de taşınsın.
        context.Request.Headers[HeaderName] = correlationId;
        accessor.CorrelationId = correlationId;

        // Response header'ı burada değil, OnStarting ile ekleniyor: Gateway senaryosunda YARP
        // zaten downstream servisin kendi response header'ını taşıyor; ikisini üst üste
        // eklemek yerine sadece henüz set edilmemişse ekliyoruz.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(HeaderName, correlationId);
            return Task.CompletedTask;
        });

        using (logger.BeginScope("CorrelationId:{CorrelationId}", correlationId))
        {
            await next(context);
        }
    }
}
