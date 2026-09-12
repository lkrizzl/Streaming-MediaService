using MediaService.Domain.Entities;
using MediaService.Persistence;
using MediaService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MediaService.Tests;

public class EpisodeMediaRepositoryTests
{
    private static MediaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MediaDbContext(options);
    }

    [Fact]
    public async Task GetByEpisodeIdAsync_ReturnsMostRecent_WhenMultipleExist()
    {
        await using var context = CreateContext();
        var episodeId = Guid.NewGuid();

        var older = new EpisodeMedia
        {
            Id = Guid.NewGuid(),
            EpisodeId = episodeId,
            ObjectKey = "old",
            Status = MediaStatus.Failed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var newer = new EpisodeMedia
        {
            Id = Guid.NewGuid(),
            EpisodeId = episodeId,
            ObjectKey = "new",
            Status = MediaStatus.Ready,
            CreatedAt = DateTime.UtcNow
        };

        context.EpisodeMedias.AddRange(older, newer);
        await context.SaveChangesAsync();

        var repository = new EpisodeMediaRepository(context);
        var result = await repository.GetByEpisodeIdAsync(episodeId);

        Assert.NotNull(result);
        Assert.Equal(newer.Id, result!.Id);
    }
}