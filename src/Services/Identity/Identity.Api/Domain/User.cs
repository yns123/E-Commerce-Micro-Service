namespace Identity.Api.Domain;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string Role { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public static User Register(string email, string role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = string.Empty,
            Role = role,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
