using System.Reflection;
using Contracts;

namespace EventBus;

public static class RoutingKeyResolver
{
    public static string GetRoutingKey(Type eventType)
    {
        var field = eventType.GetField("RoutingKey", BindingFlags.Public | BindingFlags.Static);
        if (field is null || field.GetValue(null) is not string routingKey)
        {
            throw new InvalidOperationException(
                $"{eventType.Name}, public const string RoutingKey tanımlamalı ({nameof(IntegrationEvent)} kuralı).");
        }

        return routingKey;
    }
}
