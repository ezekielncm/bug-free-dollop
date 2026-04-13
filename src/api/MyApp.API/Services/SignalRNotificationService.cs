using Microsoft.AspNetCore.SignalR;
using MyApp.Application.Common.Interfaces;
using MyApp.API.Hubs;

namespace MyApp.API.Services;

public class SignalRNotificationService(IHubContext<NotificationHub> hubContext) : INotificationService
{
    public async Task SendToUserAsync(string userId, string method, object payload, CancellationToken cancellationToken = default)
        => await hubContext.Clients.Group($"user:{userId}").SendAsync(method, payload, cancellationToken);

    public async Task SendToGroupAsync(string group, string method, object payload, CancellationToken cancellationToken = default)
        => await hubContext.Clients.Group(group).SendAsync(method, payload, cancellationToken);

    public async Task SendToAllAsync(string method, object payload, CancellationToken cancellationToken = default)
        => await hubContext.Clients.All.SendAsync(method, payload, cancellationToken);
}
