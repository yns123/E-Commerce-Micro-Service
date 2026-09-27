using System.ComponentModel.DataAnnotations;

namespace Identity.Api.Dtos;

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password);

public sealed record RegisterResponse(Guid UserId);
