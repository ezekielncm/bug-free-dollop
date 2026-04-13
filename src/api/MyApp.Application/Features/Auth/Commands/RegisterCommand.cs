using FluentValidation;
using MediatR;
using MyApp.Application.Common.Exceptions;
using MyApp.Application.Common.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Domain.Interfaces;

namespace MyApp.Application.Features.Auth.Commands;

public record RegisterCommand(string Username, string Email, string Password, string? FirstName, string? LastName) : IRequest<AuthResponse>;

public record AuthResponse(string AccessToken, string RefreshToken, Guid UserId, string Email, string Role);

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public class RegisterCommandHandler(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtService jwtService) : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            throw new ConflictException($"Email '{request.Email}' is already registered.");

        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.Create(request.Username, request.Email, passwordHash, request.FirstName, request.LastName);

        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString());
        var refreshToken = jwtService.GenerateRefreshToken();
        user.UpdateRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));

        await unitOfWork.Users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, refreshToken, user.Id, user.Email, user.Role.ToString());
    }
}
