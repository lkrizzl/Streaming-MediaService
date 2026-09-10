namespace MediaService.Application.Interfaces;

public interface IEpisodeNotifier
{
    Task NotifyMediaReadyAsync(Guid episodeId, string streamUrl, CancellationToken cancellationToken = default);
}