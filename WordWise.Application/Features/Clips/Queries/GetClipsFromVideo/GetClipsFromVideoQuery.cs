using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Clips.Dtos;
using WordWise.Application.Features.Clips.Services;
using WordWise.Application.Features.Clips.Models;
using WordWise.Domain.Entities;
using WordWise.Domain.Enums;

namespace WordWise.Application.Features.Clips.Queries.GetClipsFromVideo
{
    /// <summary>
    /// Öğrencinin kendi seçtiği YouTube videosundan alıştırma cümleleri. Videodan daha önce klip çıkarıldıysa onlar kullanılır,
    /// yoksa üretilir. Üretilen klipler yayınlanmaz (başka öğrencilere gösterilmez); yalnızca kalite kontrolünden geçenler döner.
    /// </summary>
    public record GetClipsFromVideoQuery(Guid UserId, string Url, string Mode = GetClipsFromVideoQuery.AutoMode) : IRequest<List<LearnerClipDto>>
    {
        /// <summary>Videodan en çok <see cref="AutoClipCount"/> klip (rastgele dağılmış) alınır.</summary>
        public const string AutoMode = "auto";
        /// <summary>Videonun tamamındaki uygun cümleler alınır (süre sınırı: <see cref="MaxFullVideoSeconds"/>).</summary>
        public const string AllMode = "all";
        public const int AutoClipCount = 5;
        /// <summary>
        /// "Tamamı" modunda izin verilen en uzun video. Sebep: her klip veritabanında bir satırdır (uzun videoda yüzlerce/binlerce),
        /// seçim ve kalite kontrolü klip sayısıyla orantısız artar ve tarayıcıya giden yanıt büyür.
        /// </summary>
        public const int MaxFullVideoSeconds = 3600;
    }

    public class GetClipsFromVideoQueryHandler(IWordWiseDbContext _context, ClipGenerator _generator)
        : IRequestHandler<GetClipsFromVideoQuery, List<LearnerClipDto>>
    {
        public async Task<List<LearnerClipDto>> Handle(GetClipsFromVideoQuery request, CancellationToken cancellationToken)
        {
            var youtubeId = ClipGenerator.ExtractYoutubeId(request.Url);
            if (youtubeId is null)
                throw new BusinessException("Geçerli bir YouTube bağlantısı gir (watch?v=, youtu.be veya shorts).");
            var all = string.Equals(request.Mode, GetClipsFromVideoQuery.AllMode, StringComparison.OrdinalIgnoreCase);

            var existing = await LoadPassedAsync(youtubeId, cancellationToken);
            // Otomatik modda varsayılan üretim en çok 6 klip çıkarır; ondan fazlası daha önce "tamamı" isteğiyle üretilmiş demektir.
            var haveFullSet = existing.Count > new ClipSelectorOptions().MaxClips;
            if (existing.Count == 0 || (all && !haveFullSet))
            {
                if (all)
                    await _generator.GenerateAsync(youtubeId, null, cancellationToken, autoPublish: false,
                        options: new ClipSelectorOptions { MaxClips = int.MaxValue, MinGapSec = 0 },
                        maxVideoSeconds: GetClipsFromVideoQuery.MaxFullVideoSeconds);
                else
                    await _generator.GenerateAsync(youtubeId, null, cancellationToken, autoPublish: false);

                existing = await LoadPassedAsync(youtubeId, cancellationToken);
                if (existing.Count == 0)
                    throw new BusinessException("Bu videodan uygun bir alıştırma cümlesi çıkarılamadı (altyazı yok ya da cümleler uygun değil). Başka bir video dene.");
            }

            var chosen = all || existing.Count <= GetClipsFromVideoQuery.AutoClipCount
                ? existing
                : existing.OrderBy(_ => Random.Shared.Next()).Take(GetClipsFromVideoQuery.AutoClipCount).OrderBy(c => c.StartSec).ToList();
            return chosen.Select(c => LearnerClipMapper.ToDto(c, isSaved: false)).ToList();
        }

        private Task<List<VideoClip>> LoadPassedAsync(string youtubeId, CancellationToken cancellationToken) =>
            _context.VideoClips
                .Where(c => c.YoutubeId == youtubeId && !c.IsRejected && c.QcStatus == ClipQcStatus.Passed)
                .OrderBy(c => c.StartSec)
                .ToListAsync(cancellationToken);
    }
}
