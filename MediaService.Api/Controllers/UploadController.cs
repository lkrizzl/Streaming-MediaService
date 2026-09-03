using MediaService.Application.Interfaces;
using MediaService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace MediaService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly IEpisodeMediaRepository _repository;
    private readonly IMessagePublisher _publisher;

    public UploadController(IStorageService storageService, IEpisodeMediaRepository repository, IMessagePublisher publisher)
    {
        _storageService = storageService;
        _repository = repository;
        _publisher = publisher;
    }

    [HttpPost]
    [RequestSizeLimit(500_000_000)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] Guid episodeId, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest("File is empty.");

        await using var stream = file.OpenReadStream();
        var objectKey = await _storageService.UploadAsync(stream, file.FileName, file.ContentType, cancellationToken);

        var episodeMedia = new EpisodeMedia
        {
            Id = Guid.NewGuid(),
            EpisodeId = episodeId,
            ObjectKey = objectKey,
            OriginalFileName = file.FileName,
            Status = MediaStatus.Pending
        };

        await _repository.AddAsync(episodeMedia, cancellationToken);
        await _publisher.PublishTranscodeJobAsync(episodeMedia.Id, objectKey, cancellationToken);

        return Ok(new { episodeMedia.Id, episodeMedia.ObjectKey, episodeMedia.Status });
    }

    [HttpGet("{objectKey}/url")]
    public async Task<IActionResult> GetUrl(string objectKey, CancellationToken cancellationToken)
    {
        var url = await _storageService.GetPresignedUrlAsync(objectKey, TimeSpan.FromMinutes(15), cancellationToken);
        return Ok(new { url });
    }

    [HttpGet("episode/{episodeId}")]
    public async Task<IActionResult> GetByEpisode(Guid episodeId, CancellationToken cancellationToken)
    {
        var media = await _repository.GetByEpisodeIdAsync(episodeId, cancellationToken);
        if (media is null) return NotFound();

        return Ok(media);
    }
}