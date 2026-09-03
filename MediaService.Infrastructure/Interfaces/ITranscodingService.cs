namespace MediaService.Application.Interfaces;

public interface ITranscodingService
{
    Task<string> ConvertToHlsAsync(string inputFilePath, string outputDirectory, CancellationToken cancellationToken = default);
}