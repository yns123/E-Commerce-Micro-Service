namespace Identity.Api.Dtos;

public sealed record MeResponse(Guid UserId, string Email, string Role);
