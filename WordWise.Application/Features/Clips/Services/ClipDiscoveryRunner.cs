using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Common.Settings;
using WordWise.Application.Features.Clips.Models;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Clips.Services
{
    /// <summary>
    /// Otomatik keşif ajanı: bir arama sorgusu seçer, yeni videoları bulur, her biri için klip üretir,
    /// kalite kontrolünden geçenleri (ayara göre) yayınlar ve işlenen videoyu kaydeder.
    /// </summary>
    public class ClipDiscoveryRunner(
        IWordWiseDbContext _context,
        IVideoDiscoveryService _discovery,
        ClipGenerator _generator,
        IOptions<AutoDiscoverySettings> _options,
        ILogger<ClipDiscoveryRunner> _logger)
    {
        private const int MaxQueriesPerRun = 4;

        public static readonly string[] DefaultQueries =
        {
            "english movie scene dialogue",
            "tv show scene english conversation",
            "english conversation everyday situations",
            "english interview short clip",
            "ted talk short english",
            "english speaking practice scene",
            "sitcom funny scene english",
            "movie clip english subtitles",
        };

        public async Task<DiscoveryRunResult> RunOnceAsync(CancellationToken cancellationToken)
        {
            var settings = _options.Value;
            var queries = settings.Queries.Where(q => !string.IsNullOrWhiteSpace(q)).ToList();
            if (queries.Count == 0) queries = DefaultQueries.ToList();
            // Tek sorgu çoğu zaman zaten bilinen videoları döndürür; yeterli yeni video bulunana kadar (en çok 4) farklı sorgu denenir.
            var wanted = Math.Max(1, settings.MaxVideosPerRun);
            var pool = new Dictionary<string, DiscoveredVideoInfo>();
            var foundBy = new Dictionary<string, string>();
            var usedQueries = new List<string>();
            var fresh = new List<DiscoveredVideoInfo>();
            foreach (var candidateQuery in queries.OrderBy(_ => Random.Shared.Next()).Take(MaxQueriesPerRun))
            {
                var found = await _discovery.SearchAsync(candidateQuery, settings.MaxResultsPerQuery, settings.VideoDuration, cancellationToken);
                usedQueries.Add(candidateQuery);
                foreach (var video in found)
                    if (pool.TryAdd(video.YoutubeId, video)) foundBy[video.YoutubeId] = candidateQuery;

                var ids = pool.Keys.ToList();
                var known = await _context.DiscoveredVideos
                    .Where(d => ids.Contains(d.YoutubeId)).Select(d => d.YoutubeId).ToListAsync(cancellationToken);
                var withClips = await _context.VideoClips
                    .Where(c => ids.Contains(c.YoutubeId)).Select(c => c.YoutubeId).Distinct().ToListAsync(cancellationToken);

                fresh = pool.Values
                    .Where(f => !known.Contains(f.YoutubeId) && !withClips.Contains(f.YoutubeId))
                    .Take(wanted)
                    .ToList();
                if (fresh.Count >= wanted) break;
            }
            var query = string.Join(" | ", usedQueries);

            var processed = 0; var created = 0; var published = 0;
            var failures = new List<string>();

            foreach (var video in fresh)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var record = new DiscoveredVideo
                {
                    YoutubeId = video.YoutubeId,
                    Title = video.Title.Length > 500 ? video.Title[..500] : video.Title,
                    SearchQuery = foundBy[video.YoutubeId].Length > 300 ? foundBy[video.YoutubeId][..300] : foundBy[video.YoutubeId],
                };

                try
                {
                    var clips = await _generator.GenerateAsync(video.YoutubeId, null, cancellationToken, settings.AutoPublish, settings.AutoPublishRequiresRealTimings,
                        options: ClipSelectorOptions.ForWholeVideo(Math.Max(1, settings.MaxClipsPerVideo)),
                        maxVideoSeconds: Math.Max(1, settings.MaxVideoMinutes) * 60);
                    record.Status = "Done";
                    record.ClipCount = clips.Count;
                    record.PublishedCount = clips.Count(c => c.IsPublished);
                    created += record.ClipCount;
                    published += record.PublishedCount;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    record.Status = "Failed";
                    record.Error = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                    failures.Add($"{video.YoutubeId}: {ex.Message}");
                    _logger.LogWarning(ex, "Otomatik keşif: {VideoId} işlenemedi.", video.YoutubeId);
                }

                processed++;
                await _context.DiscoveredVideos.AddAsync(record, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new DiscoveryRunResult(query, pool.Count, processed, created, published, failures);
        }
    }
}
