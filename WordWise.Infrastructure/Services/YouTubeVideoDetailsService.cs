using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Xml;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Common.Settings;
using WordWise.Application.Features.Clips.Models;

namespace WordWise.Infrastructure.Services
{
    public class YouTubeVideoDetailsService(
        HttpClient _httpClient,
        IOptions<YoutubeSettings> _options,
        ILogger<YouTubeVideoDetailsService> _logger) : IVideoDetailsService
    {
        public async Task<VideoDetails?> GetVideoDetailsAsync(string youtubeId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "https://www.googleapis.com/youtube/v3/videos" +
                          "?part=snippet,contentDetails,status" +
                          $"&id={Uri.EscapeDataString(youtubeId)}" +
                          $"&key={_options.Value.ApiKey}";

                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("YouTube videos API hatası. Status: {Status}", response.StatusCode);
                    return null;
                }

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                if (!doc.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
                    return null;

                return Parse(youtubeId, items[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{VideoId} için video detayları alınırken hata oluştu.", youtubeId);
                return null;
            }
        }

        public static VideoDetails Parse(string youtubeId, JsonElement item)
        {
            var snippet = item.TryGetProperty("snippet", out var s) ? s : default;
            var content = item.TryGetProperty("contentDetails", out var c) ? c : default;
            var status = item.TryGetProperty("status", out var st) ? st : default;

            string? Str(JsonElement el, string name) =>
                el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
                    ? v.GetString() : null;

            var tags = new List<string>();
            if (snippet.ValueKind == JsonValueKind.Object && snippet.TryGetProperty("tags", out var tagEl) && tagEl.ValueKind == JsonValueKind.Array)
                tags.AddRange(tagEl.EnumerateArray().Select(t => t.GetString() ?? "").Where(t => t.Length > 0));

            var seconds = 0;
            var iso = Str(content, "duration");
            if (!string.IsNullOrEmpty(iso))
            {
                try { seconds = (int)XmlConvert.ToTimeSpan(iso).TotalSeconds; } catch (FormatException) { }
            }

            var ageRestricted = content.ValueKind == JsonValueKind.Object
                && content.TryGetProperty("contentRating", out var rating)
                && rating.ValueKind == JsonValueKind.Object
                && Str(rating, "ytRating") == "ytAgeRestricted";

            var embeddable = status.ValueKind == JsonValueKind.Object
                && status.TryGetProperty("embeddable", out var emb)
                && emb.ValueKind == JsonValueKind.True;

            return new VideoDetails(
                youtubeId,
                Str(snippet, "title") ?? string.Empty,
                Str(snippet, "description"),
                Str(snippet, "channelTitle"),
                tags,
                seconds,
                Str(content, "definition") ?? "sd",
                embeddable,
                Str(status, "privacyStatus") ?? "unknown",
                ageRestricted);
        }
    }
}
