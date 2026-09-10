using MediaService.Application.Interfaces;
using MediaService.Application.Messages;
using MediaService.Domain.Entities;
using MediaService.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MediaService.Worker.Consumers;

public class TranscodeJobConsumer : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TranscodeJobConsumer> _logger;
    private readonly string _mediaServicePublicUrl;

    public TranscodeJobConsumer(IOptions<RabbitMqOptions> options, IServiceScopeFactory scopeFactory, ILogger<TranscodeJobConsumer> logger, IConfiguration configuration)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _mediaServicePublicUrl = configuration["MediaServicePublicUrl"]!;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            UserName = _options.UserName,
            Password = _options.Password
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(_options.QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var message = JsonSerializer.Deserialize<TranscodeJobMessage>(body)!;

            _logger.LogInformation("Received transcode job for EpisodeMediaId={EpisodeMediaId}, ObjectKey={ObjectKey}", message.EpisodeMediaId, message.ObjectKey);

            await ProcessJobAsync(message, stoppingToken);

            await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
        };

        await channel.BasicConsumeAsync(_options.QueueName, autoAck: false, consumer, stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessJobAsync(TranscodeJobMessage message, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEpisodeMediaRepository>();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var transcoder = scope.ServiceProvider.GetRequiredService<ITranscodingService>();
        var notifier = scope.ServiceProvider.GetRequiredService<IEpisodeNotifier>();

        var episodeMedia = await repository.GetByIdAsync(message.EpisodeMediaId, cancellationToken);
        if (episodeMedia is null)
        {
            _logger.LogWarning("EpisodeMedia {EpisodeMediaId} not found, skipping job", message.EpisodeMediaId);
            return;
        }

        episodeMedia.Status = MediaStatus.Processing;
        await repository.UpdateAsync(episodeMedia, cancellationToken);

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var inputPath = Path.Combine(tempDir, "input");
            await storage.DownloadToFileAsync(message.ObjectKey, inputPath, cancellationToken);

            var outputDir = Path.Combine(tempDir, "hls");
            await transcoder.ConvertToHlsAsync(inputPath, outputDir, cancellationToken);

            var version = Guid.NewGuid();
            var hlsPrefix = $"hls/{episodeMedia.EpisodeId}/{episodeMedia.Id}/{version}";
            await storage.UploadDirectoryAsync(outputDir, hlsPrefix, cancellationToken);
            await storage.DeleteAsync(message.ObjectKey, cancellationToken);

            episodeMedia.Status = MediaStatus.Ready;
            episodeMedia.StreamUrl = $"{hlsPrefix}/playlist.m3u8";

            var publicStreamUrl = $"{_mediaServicePublicUrl}/api/upload/episode/{episodeMedia.EpisodeId}/stream";
            await notifier.NotifyMediaReadyAsync(episodeMedia.EpisodeId, publicStreamUrl, cancellationToken);

            _logger.LogInformation("Transcoding completed and WebApi notified for EpisodeMediaId={EpisodeMediaId}", episodeMedia.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcoding failed for EpisodeMediaId={EpisodeMediaId}", episodeMedia.Id);
            episodeMedia.Status = MediaStatus.Failed;
        }
        finally
        {
            Directory.Delete(tempDir, true);
            await repository.UpdateAsync(episodeMedia, cancellationToken);
        }
    }
}