using MyApp.Domain.Common;
using MyApp.Domain.Enums;
using MyApp.Domain.Events;

namespace MyApp.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public UserRole Role { get; private set; } = UserRole.User;
    public bool IsActive { get; private set; } = true;
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiry { get; private set; }

    private User() { }

    public static User Create(string username, string email, string passwordHash,
        string? firstName = null, string? lastName = null)
    {
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName
        };
        user.AddDomainEvent(new UserCreatedEvent(user.Id, user.Email));
        return user;
    }

    public void UpdateRefreshToken(string token, DateTime expiry)
    {
        RefreshToken = token;
        RefreshTokenExpiry = expiry;
        SetUpdatedAt();
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiry = null;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }
}
