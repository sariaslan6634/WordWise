using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Common.Settings;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Clips.Services
{
    /// <param name="AllowedPerWindow">Pencere başına butonla kullanılabilen tıklama sayısı (toplam hak - yedek).</param>
    /// <param name="NextAvailableAt">Hak bittiyse bir hakkın serbest kalacağı an (UTC); hak varsa null.</param>
    /// <param name="VideosPerClick">Her tıklamada işlenecek en çok video sayısı.</param>
    /// <param name="VideosInWindow">Penceredeki tıklamalarla işlenen toplam video.</param>
    /// <param name="ClipsInWindow">Penceredeki tıklamalarla üretilen toplam klip.</param>
    public record DiscoveryRunStatus(
        int AllowedPerWindow, int Used, int Remaining, int WindowHours, DateTimeOffset? NextAvailableAt, DateTimeOffset ServerNow,
        int VideosPerClick, int VideosInWindow, int ClipsInWindow, int PublishedInWindow);

    /// <summary>
    /// Admin "Klip getir" butonunun günlük tıklama hakkı. YouTube API kotası sınırlı olduğu için kayan bir pencerede
    /// (varsayılan 24 saat) en çok (ManualRunLimit - ManualRunReserve) tıklamaya izin verilir; yedek hak hep içeride kalır.
    /// </summary>
    public class DiscoveryRunQuota(IWordWiseDbContext _context, IOptions<AutoDiscoverySettings> _options)
    {
        public async Task<DiscoveryRunStatus> GetStatusAsync(DateTime utcNow, CancellationToken cancellationToken)
        {
            var settings = _options.Value;
            var window = TimeSpan.FromHours(Math.Max(1, settings.ManualRunWindowHours));
            var allowed = Math.Max(0, settings.ManualRunLimit - Math.Max(0, settings.ManualRunReserve));

            var since = utcNow - window;
            var logs = await _context.DiscoveryRunLogs
                .Where(l => l.CreatedAt > since)
                .OrderBy(l => l.CreatedAt)
                .ToListAsync(cancellationToken);
            var times = logs.Select(l => l.CreatedAt).ToList();

            var used = times.Count;
            var remaining = Math.Max(0, allowed - used);
            DateTimeOffset? next = null;
            if (remaining == 0)
            {
                // Bir hak, (kullanılan - izin verilen + 1). en eski tıklama pencereden çıkınca serbest kalır.
                var index = Math.Clamp(used - allowed, 0, Math.Max(0, used - 1));
                if (used > 0) next = Utc(times[index] + window);
                else next = Utc(utcNow + window); // allowed = 0: buton kapalı
            }
            return new DiscoveryRunStatus(allowed, used, remaining, (int)window.TotalHours, next, Utc(utcNow),
                Math.Max(1, settings.MaxVideosPerRun), logs.Sum(l => l.VideosProcessed), logs.Sum(l => l.ClipsCreated), logs.Sum(l => l.ClipsPublished));
        }

        /// <summary>Bir tıklama hakkı harcar; hak yoksa ne zaman tekrar tıklanabileceğini söyleyen hata fırlatır.</summary>
        public async Task<DiscoveryRunLog> ConsumeAsync(DateTime utcNow, CancellationToken cancellationToken)
        {
            var status = await GetStatusAsync(utcNow, cancellationToken);
            if (status.Remaining <= 0)
            {
                var wait = status.NextAvailableAt is { } at ? at - Utc(utcNow) : TimeSpan.FromHours(status.WindowHours);
                if (wait < TimeSpan.Zero) wait = TimeSpan.Zero;
                throw new BusinessException(
                    $"YouTube kotasını korumak için {status.WindowHours} saatte en çok {status.AllowedPerWindow} kez klip getirilebilir. " +
                    $"Tekrar denemek için {(int)wait.TotalHours} saat {wait.Minutes} dakika bekle.");
            }

            var log = new DiscoveryRunLog { CreatedAt = utcNow };
            _context.DiscoveryRunLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
            return log;
        }

        /// <summary>Çalıştırma bitince kaç video/klip çekildiğini tıklama kaydına yazar.</summary>
        public async Task RecordResultAsync(DiscoveryRunLog log, int videosProcessed, int clipsCreated, int clipsPublished, int failedVideos, CancellationToken cancellationToken)
        {
            log.VideosProcessed = videosProcessed;
            log.ClipsCreated = clipsCreated;
            log.ClipsPublished = clipsPublished;
            log.FailedVideos = failedVideos;
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
