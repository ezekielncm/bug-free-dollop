using MediatR;
using MyApp.Application.Common.Exceptions;
using MyApp.Domain.Interfaces;

namespace MyApp.Application.Features.Users.Queries;

public record GetUserByIdQuery(Guid UserId) : IRequest<UserDto>;

public record UserDto(Guid Id, string Username, string Email, string? FirstName, string? LastName, string Role, bool IsActive, DateTime CreatedAt);

public class GetUserByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), request.UserId);

        return new UserDto(user.Id, user.Username, user.Email, user.FirstName, user.LastName,
            user.Role.ToString(), user.IsActive, user.CreatedAt);
    }
}
