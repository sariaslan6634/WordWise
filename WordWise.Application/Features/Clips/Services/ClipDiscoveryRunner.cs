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
            var query = queries[Random.Shared.Next(queries.Count)];

            var found = await _discovery.SearchAsync(query, settings.MaxResultsPerQuery, settings.VideoDuration, cancellationToken);
            var unique = found.GroupBy(f => f.YoutubeId).Select(g => g.First()).ToList();
            var ids = unique.Select(f => f.YoutubeId).ToList();

            var known = await _context.DiscoveredVideos
                .Where(d => ids.Contains(d.YoutubeId)).Select(d => d.YoutubeId).ToListAsync(cancellationToken);
            var withClips = await _context.VideoClips
                .Where(c => ids.Contains(c.YoutubeId)).Select(c => c.YoutubeId).Distinct().ToListAsync(cancellationToken);

            var fresh = unique
                .Where(f => !known.Contains(f.YoutubeId) && !withClips.Contains(f.YoutubeId))
                .Take(Math.Max(1, settings.MaxVideosPerRun))
                .ToList();

            var processed = 0; var created = 0; var published = 0;
            var failures = new List<string>();

            foreach (var video in fresh)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var record = new DiscoveredVideo
                {
                    YoutubeId = video.YoutubeId,
                    Title = video.Title.Length > 500 ? video.Title[..500] : video.Title,
                    SearchQuery = query.Length > 300 ? query[..300] : query,
                };

                try
                {
                    var clips = await _generator.GenerateAsync(video.YoutubeId, null, cancellationToken, settings.AutoPublish, settings.AutoPublishRequiresRealTimings);
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

            return new DiscoveryRunResult(query, unique.Count, processed, created, published, failures);
        }
    }
}
