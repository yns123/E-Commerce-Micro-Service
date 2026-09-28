using System.Text.Json;
using Common.Correlation;
using Contracts;
using EventBus.Outbox;
using Microsoft.EntityFrameworkCore;
using Ordering.Api.Data;

namespace Ordering.Tests;

internal static class OutboxTestHelpers
{
    public static IOutbox CreateOutbox(OrderingDbContext db) => new EfOutbox<OrderingDbContext>(db, new CorrelationIdAccessor());

    public static async Task<IReadOnlyList<IntegrationEvent>> GetOutboxEventsAsync(OrderingDbContext db)
    {
        var messages = await db.OutboxMessages.AsNoTracking().ToListAsync();
        return messages
            .Select(m => (IntegrationEvent)JsonSerializer.Deserialize(m.Content, Type.GetType(m.Type)!, new JsonSerializerOptions(JsonSerializerDefaults.Web))!)
            .ToList();
    }
}
