using System.Text.Json;

namespace EventBus;

internal static class EventBusJsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web);
}
