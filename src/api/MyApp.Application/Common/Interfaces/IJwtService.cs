namespace MyApp.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email, string role);
    string GenerateRefreshToken();
    Guid? ValidateToken(string token);
}
