namespace MediaService.Application.Interfaces;

public interface IMessagePublisher
{
    Task PublishTranscodeJobAsync(Guid episodeMediaId, string objectKey, CancellationToken cancellationToken = default);
}