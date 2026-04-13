using FluentValidation;
using MediatR;
using MyApp.Application.Common.Exceptions;
using MyApp.Application.Common.Interfaces;
using MyApp.Domain.Interfaces;

namespace MyApp.Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtService jwtService) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var refreshToken = jwtService.GenerateRefreshToken();
        user.UpdateRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));

        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, refreshToken, user.Id, user.Email, user.Role.ToString());
    }
}
