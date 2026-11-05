using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Common.Settings;
using WordWise.Application.Features.Clips.Models;

namespace WordWise.Infrastructure.Services
{
    public class YouTubeDiscoveryService(
        HttpClient _httpClient,
        IOptions<YoutubeSettings> _options,
        ILogger<YouTubeDiscoveryService> _logger) : IVideoDiscoveryService
    {
        public async Task<List<DiscoveredVideoInfo>> SearchAsync(
            string query, int maxResults, string videoDuration, CancellationToken cancellationToken = default)
        {
            var duration = videoDuration is "short" or "medium" or "long" ? videoDuration : "any";
            var url = "https://www.googleapis.com/youtube/v3/search" +
                      "?part=snippet&type=video" +
                      $"&q={Uri.EscapeDataString(query)}" +
                      $"&maxResults={Math.Clamp(maxResults, 1, 50)}" +
                      "&relevanceLanguage=en&videoCaption=closedCaption&videoEmbeddable=true&safeSearch=strict" +
                      $"&videoDuration={duration}" +
                      $"&key={_options.Value.ApiKey}";

            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("YouTube keşif araması hatası. Status: {Status}", response.StatusCode);
                    return new List<DiscoveredVideoInfo>();
                }

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                var result = new List<DiscoveredVideoInfo>();
                if (!doc.RootElement.TryGetProperty("items", out var items)) return result;

                foreach (var item in items.EnumerateArray())
                {
                    var id = item.TryGetProperty("id", out var idEl) && idEl.TryGetProperty("videoId", out var v) ? v.GetString() : null;
                    var title = item.TryGetProperty("snippet", out var sn) && sn.TryGetProperty("title", out var t) ? t.GetString() : null;
                    if (!string.IsNullOrEmpty(id)) result.Add(new DiscoveredVideoInfo(id, title ?? id));
                }
                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "YouTube keşif araması sırasında hata oluştu.");
                return new List<DiscoveredVideoInfo>();
            }
        }
    }
}
