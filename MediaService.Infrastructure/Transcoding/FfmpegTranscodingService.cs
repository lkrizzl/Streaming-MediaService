using System.Diagnostics;
using MediaService.Application.Interfaces;

namespace MediaService.Infrastructure.Transcoding;

public class FfmpegTranscodingService : ITranscodingService
{
    public async Task<string> ConvertToHlsAsync(string inputFilePath, string outputDirectory, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);
        var playlistPath = Path.Combine(outputDirectory, "playlist.m3u8");

        var arguments = $"-i \"{inputFilePath}\" -codec:v libx264 -codec:a aac -hls_time 10 -hls_playlist_type vod -hls_segment_filename \"{outputDirectory}/segment_%03d.ts\" \"{playlistPath}\"";

        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"ffmpeg failed: {error}");
        }

        return playlistPath;
    }
}