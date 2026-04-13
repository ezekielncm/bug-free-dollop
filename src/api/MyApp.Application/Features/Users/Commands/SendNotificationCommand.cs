using FluentValidation;
using MediatR;
using MyApp.Application.Common.Exceptions;
using MyApp.Application.Common.Interfaces;
using MyApp.Domain.Interfaces;

namespace MyApp.Application.Features.Users.Commands;

public record SendNotificationCommand(string UserId, string Message) : IRequest;

public class SendNotificationCommandHandler(INotificationService notificationService) : IRequestHandler<SendNotificationCommand>
{
    public async Task Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        await notificationService.SendToUserAsync(request.UserId, "Notification",
            new { message = request.Message, timestamp = DateTime.UtcNow }, cancellationToken);
    }
}
