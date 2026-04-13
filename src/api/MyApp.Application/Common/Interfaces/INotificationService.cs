namespace MyApp.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendToUserAsync(string userId, string method, object payload, CancellationToken cancellationToken = default);
    Task SendToGroupAsync(string group, string method, object payload, CancellationToken cancellationToken = default);
    Task SendToAllAsync(string method, object payload, CancellationToken cancellationToken = default);
}
