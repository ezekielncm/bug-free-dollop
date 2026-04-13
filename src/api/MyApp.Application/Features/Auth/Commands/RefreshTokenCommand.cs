using MediatR;
using MyApp.Application.Common.Exceptions;
using MyApp.Application.Common.Interfaces;
using MyApp.Domain.Interfaces;

namespace MyApp.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

public class RefreshTokenCommandHandler(
    IUnitOfWork unitOfWork,
    IJwtService jwtService) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token expired.");

        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var newRefreshToken = jwtService.GenerateRefreshToken();
        user.UpdateRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7));

        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, newRefreshToken, user.Id, user.Email, user.Role.ToString());
    }
}
