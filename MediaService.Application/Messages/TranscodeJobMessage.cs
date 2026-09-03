namespace MediaService.Application.Messages;

public class TranscodeJobMessage
{
    public Guid EpisodeMediaId { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
}