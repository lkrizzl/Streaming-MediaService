using MediaService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediaService.Persistence;

public class MediaDbContext : DbContext
{
    public MediaDbContext(DbContextOptions<MediaDbContext> options) : base(options) { }

    public DbSet<EpisodeMedia> EpisodeMedias => Set<EpisodeMedia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EpisodeMedia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ObjectKey).IsRequired();
            entity.Property(e => e.OriginalFileName).IsRequired();
            entity.HasIndex(e => e.EpisodeId);
        });
    }
}