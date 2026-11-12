using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Clips.Dtos;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Clips.Services
{
    /// <summary>
    /// Kanal adı/açıklaması kaydedilmeden önce üretilmiş eski kliplerin kaynak bilgisini YouTube'dan tamamlar.
    /// Her çağrıda en çok <see cref="MaxVideosPerCall"/> video sorgulanır; kalanlar sonraki isteklerde tamamlanır.
    /// </summary>
    public class ClipSourceBackfiller(IWordWiseDbContext _context, IVideoDetailsService _details)
    {
        public const int MaxVideosPerCall = 4;

        public async Task FillAsync(IReadOnlyCollection<LearnerClipDto> clips, CancellationToken cancellationToken)
        {
            var missing = clips.Where(c => string.IsNullOrEmpty(c.SourceChannel)).GroupBy(c => c.YoutubeId).Take(MaxVideosPerCall).ToList();
            if (missing.Count == 0) return;

            foreach (var group in missing)
            {
                VideoDetailsLite? info = null;
                try
                {
                    var details = await _details.GetSnippetAsync(group.Key, cancellationToken);
                    if (details is not null && !string.IsNullOrWhiteSpace(details.ChannelTitle))
                        info = new VideoDetailsLite(Trim(details.Title, 500), Trim(details.ChannelTitle, 200));
                }
                catch (Exception) { /* YouTube'a ulaşılamadı: sonraki istekte tekrar denenir */ }
                if (info is null) continue;

                foreach (var dto in group)
                {
                    dto.SourceChannel = info.Channel;
                    dto.SourceTitle ??= info.Title;
                }

                var rows = await _context.VideoClips.Where(c => c.YoutubeId == group.Key).ToListAsync(cancellationToken);
                foreach (var row in rows)
                {
                    row.SourceChannel ??= info.Channel;
                    row.SourceTitle ??= info.Title;
                }
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private static string? Trim(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            return value.Length > max ? value[..max] : value;
        }

        private sealed record VideoDetailsLite(string? Title, string? Channel);
    }
}
