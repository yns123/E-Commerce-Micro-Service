namespace Contracts;

public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email) : IntegrationEvent
{
    public const string RoutingKey = "identity.user.registered";
}
