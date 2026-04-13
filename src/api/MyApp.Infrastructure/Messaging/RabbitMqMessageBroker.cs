using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyApp.Application.Common.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MyApp.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
}

public class RabbitMqMessageBroker : IMessageBroker, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _publishChannel;
    private readonly ILogger<RabbitMqMessageBroker> _logger;

    public RabbitMqMessageBroker(IOptions<RabbitMqOptions> options, ILogger<RabbitMqMessageBroker> logger)
    {
        _logger = logger;
        var opts = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = opts.Host,
            Port = opts.Port,
            UserName = opts.Username,
            Password = opts.Password,
            VirtualHost = opts.VirtualHost,
            AutomaticRecoveryEnabled = true
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _publishChannel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        await _publishChannel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        var props = new BasicProperties { ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent };
        await _publishChannel.BasicPublishAsync(exchange, routingKey, true, props, body, cancellationToken);
        _logger.LogDebug("Published message to {Exchange}/{RoutingKey}", exchange, routingKey);
    }

    public async Task SubscribeAsync<T>(string queue, Func<T, Task> handler, CancellationToken cancellationToken = default)
    {
        var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<T>(body);
                if (message is not null)
                    await handler(message);
                await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue {Queue}", queue);
                await channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);
            }
        };

        await channel.BasicConsumeAsync(queue, false, consumer, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _publishChannel.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
