using System.Text;
using System.Text.Json;
using MediaService.Application.Interfaces;
using MediaService.Application.Messages;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MediaService.Infrastructure.Messaging;

public class RabbitMqPublisher : IMessagePublisher
{
    private readonly RabbitMqOptions _options;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public async Task PublishTranscodeJobAsync(Guid episodeMediaId, string objectKey, CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            UserName = _options.UserName,
            Password = _options.Password
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(_options.QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);

        var message = new TranscodeJobMessage { EpisodeMediaId = episodeMediaId, ObjectKey = objectKey };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: _options.QueueName, body: body, cancellationToken: cancellationToken);
    }
}