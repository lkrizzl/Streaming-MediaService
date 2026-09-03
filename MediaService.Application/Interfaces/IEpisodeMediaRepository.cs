using MediaService.Domain.Entities;

namespace MediaService.Application.Interfaces;

public interface IEpisodeMediaRepository
{
    Task<EpisodeMedia?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EpisodeMedia?> GetByEpisodeIdAsync(Guid episodeId, CancellationToken cancellationToken = default);
    Task AddAsync(EpisodeMedia entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(EpisodeMedia entity, CancellationToken cancellationToken = default);
}