using System.ComponentModel.DataAnnotations;

namespace Identity.Api.Dtos;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record LoginResponse(string Token, DateTime ExpiresAt, string Email, string Role);
