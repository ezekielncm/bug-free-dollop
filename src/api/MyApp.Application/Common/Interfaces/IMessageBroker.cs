namespace MyApp.Application.Common.Interfaces;

public interface IMessageBroker
{
    Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default);
    Task SubscribeAsync<T>(string queue, Func<T, Task> handler, CancellationToken cancellationToken = default);
}
