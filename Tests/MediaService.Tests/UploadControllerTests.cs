using MediaService.Api.Controllers;
using MediaService.Application.Interfaces;
using MediaService.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MediaService.Tests;

public class UploadControllerTests
{
    [Fact]
    public async Task Upload_UpdatesExistingRecord_WhenEpisodeAlreadyHasMedia()
    {
        var episodeId = Guid.NewGuid();
        var existingId = Guid.NewGuid();
        var existing = new EpisodeMedia
        {
            Id = existingId,
            EpisodeId = episodeId,
            ObjectKey = "old_object",
            Status = MediaStatus.Ready,
            StreamUrl = "hls/old/playlist.m3u8"
        };

        var storage = new Mock<IStorageService>();
        storage.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync("new_object");

        var repository = new Mock<IEpisodeMediaRepository>();
        repository.Setup(r => r.GetByEpisodeIdAsync(episodeId, default)).ReturnsAsync(existing);

        var publisher = new Mock<IMessagePublisher>();

        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["InternalApiKey"]).Returns("test-key");

        var controller = new UploadController(storage.Object, repository.Object, publisher.Object, configuration.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.ControllerContext.HttpContext.Request.Headers["X-Internal-Api-Key"] = "test-key";

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(10);
        fileMock.Setup(f => f.FileName).Returns("new.mkv");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[10]));

        await controller.Upload(fileMock.Object, episodeId, default);

        repository.Verify(r => r.UpdateAsync(It.Is<EpisodeMedia>(e => e.Id == existingId && e.Status == MediaStatus.Pending), default), Times.Once);
        repository.Verify(r => r.AddAsync(It.IsAny<EpisodeMedia>(), default), Times.Never);
        publisher.Verify(p => p.PublishTranscodeJobAsync(existingId, "new_object", default), Times.Once);
    }

    [Fact]
    public async Task Upload_CreatesNewRecord_WhenNoExistingMedia()
    {
        var episodeId = Guid.NewGuid();

        var storage = new Mock<IStorageService>();
        storage.Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync("new_object");

        var repository = new Mock<IEpisodeMediaRepository>();
        repository.Setup(r => r.GetByEpisodeIdAsync(episodeId, default)).ReturnsAsync((EpisodeMedia?)null);

        var publisher = new Mock<IMessagePublisher>();

        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["InternalApiKey"]).Returns("test-key");

        var controller = new UploadController(storage.Object, repository.Object, publisher.Object, configuration.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.ControllerContext.HttpContext.Request.Headers["X-Internal-Api-Key"] = "test-key";

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(10);
        fileMock.Setup(f => f.FileName).Returns("new.mkv");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[10]));

        await controller.Upload(fileMock.Object, episodeId, default);

        repository.Verify(r => r.AddAsync(It.IsAny<EpisodeMedia>(), default), Times.Once);
    }
}