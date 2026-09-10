using MediaService.Application.Interfaces;
using MediaService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Persistence.Repositories;

public class EpisodeMediaRepository : IEpisodeMediaRepository
{
    private readonly MediaDbContext _context;

    public EpisodeMediaRepository(MediaDbContext context)
    {
        _context = context;
    }

    public Task<EpisodeMedia?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.EpisodeMedias.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<EpisodeMedia?> GetByEpisodeIdAsync(Guid episodeId, CancellationToken cancellationToken = default)
        => _context.EpisodeMedias
            .Where(e => e.EpisodeId == episodeId)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(EpisodeMedia entity, CancellationToken cancellationToken = default)
    {
        await _context.EpisodeMedias.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(EpisodeMedia entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.EpisodeMedias.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}