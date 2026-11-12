using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
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
        public Task<VideoDetails?> GetVideoDetailsAsync(string youtubeId, CancellationToken cancellationToken = default) =>
            FetchAsync(youtubeId, probeResolution: true, cancellationToken);

        public Task<VideoDetails?> GetSnippetAsync(string youtubeId, CancellationToken cancellationToken = default) =>
            FetchAsync(youtubeId, probeResolution: false, cancellationToken);

        private async Task<VideoDetails?> FetchAsync(string youtubeId, bool probeResolution, CancellationToken cancellationToken)
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

                var details = Parse(youtubeId, items[0]);

                // YouTube API sadece hd/sd söyler; HD değilse gerçek yüksekliği yt-dlp ile ölç (480p eşiği için).
                if (probeResolution && !string.Equals(details.Definition, "hd", StringComparison.OrdinalIgnoreCase))
                    details = details with { MaxHeight = await ProbeMaxHeightAsync(youtubeId, cancellationToken) };

                return details;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{VideoId} için video detayları alınırken hata oluştu.", youtubeId);
                return null;
            }
        }

        private async Task<int?> ProbeMaxHeightAsync(string youtubeId, CancellationToken ct)
        {
            try
            {
                var exe = string.IsNullOrWhiteSpace(_options.Value.YtDlpPath) ? "yt-dlp" : _options.Value.YtDlpPath;
                var psi = new ProcessStartInfo(exe)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                };
                foreach (var arg in new[] { "--dump-single-json", "--no-playlist", "--no-warnings", "--skip-download", $"https://www.youtube.com/watch?v={youtubeId}" })
                    psi.ArgumentList.Add(arg);

                using var process = Process.Start(psi);
                if (process is null) return null;

                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(60));
                var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
                _ = process.StandardError.ReadToEndAsync(timeout.Token);
                await process.WaitForExitAsync(timeout.Token);
                if (process.ExitCode != 0) return null;

                using var doc = JsonDocument.Parse(await output);
                return MaxHeightFromFormats(doc.RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[yt-dlp] {VideoId} için çözünürlük ölçülemedi.", youtubeId);
                return null;
            }
        }

        /// <summary>yt-dlp JSON çıktısındaki video biçimlerinin en yükseğini döndürür.</summary>
        public static int? MaxHeightFromFormats(JsonElement root)
        {
            if (!root.TryGetProperty("formats", out var formats) || formats.ValueKind != JsonValueKind.Array) return null;
            int? max = null;
            foreach (var f in formats.EnumerateArray())
            {
                if (f.TryGetProperty("vcodec", out var vc) && vc.ValueKind == JsonValueKind.String && vc.GetString() == "none") continue;
                if (f.TryGetProperty("height", out var h) && h.ValueKind == JsonValueKind.Number && h.TryGetInt32(out var v) && (max is null || v > max))
                    max = v;
            }
            return max;
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
