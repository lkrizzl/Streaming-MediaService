using System.Net.Http.Json;
using MediaService.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace MediaService.Infrastructure.Webhooks;

public class WebApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string InternalApiKey { get; set; } = string.Empty;
}

public class WebApiNotifier : IEpisodeNotifier
{
    private readonly HttpClient _httpClient;
    private readonly WebApiOptions _options;

    public WebApiNotifier(HttpClient httpClient, IOptions<WebApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task NotifyMediaReadyAsync(Guid episodeId, string streamUrl, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/api/internal/episodes/{episodeId}/media-ready")
        {
            Content = JsonContent.Create(new { StreamUrl = streamUrl })
        };
        request.Headers.Add("X-Internal-Api-Key", _options.InternalApiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}