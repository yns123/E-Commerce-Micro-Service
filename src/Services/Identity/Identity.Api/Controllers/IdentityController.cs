using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Contracts;
using EventBus;
using Identity.Api.Auth;
using Identity.Api.Data;
using Identity.Api.Domain;
using Identity.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DomainUser = Identity.Api.Domain.User;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/identity")]
public sealed class IdentityController(
    IdentityDbContext db,
    IEventBus eventBus,
    JwtTokenGenerator tokenGenerator) : ControllerBase
{
    private static readonly PasswordHasher<DomainUser> Hasher = new();

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email, ct))
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Bu e-posta zaten kayıtlı.");

        var user = DomainUser.Register(request.Email, Roles.Customer);
        user.SetPasswordHash(Hasher.HashPassword(user, request.Password));

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        await eventBus.PublishAsync(new UserRegisteredIntegrationEvent(user.Id, user.Email), ct);

        return StatusCode(StatusCodes.Status201Created, new RegisterResponse(user.Id));
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user is null || Hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return Unauthorized();

        var (token, expiresAt) = tokenGenerator.GenerateToken(user);

        return Ok(new LoginResponse(token, expiresAt, user.Email, user.Role));
    }

    [HttpGet("me"), Authorize]
    public ActionResult<MeResponse> Me()
    {
        var userId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var email = User.FindFirstValue(JwtRegisteredClaimNames.Email)!;
        var role = User.FindFirstValue("role")!;

        return Ok(new MeResponse(userId, email, role));
    }
}
