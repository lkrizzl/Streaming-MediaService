namespace MediaService.Domain.Entities;

public enum MediaStatus
{
    Pending,
    Processing,
    Ready,
    Failed
}

public class EpisodeMedia
{
    public Guid Id { get; set; }
    public Guid EpisodeId { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public MediaStatus Status { get; set; } = MediaStatus.Pending;
    public string? StreamUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}