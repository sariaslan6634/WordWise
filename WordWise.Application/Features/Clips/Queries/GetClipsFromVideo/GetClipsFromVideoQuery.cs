using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Clips.Dtos;
using WordWise.Application.Features.Clips.Services;
using WordWise.Domain.Enums;

namespace WordWise.Application.Features.Clips.Queries.GetClipsFromVideo
{
    /// <summary>
    /// Öğrencinin kendi seçtiği YouTube videosundan alıştırma cümleleri. Videodan daha önce klip çıkarıldıysa onlar kullanılır,
    /// yoksa üretilir. Üretilen klipler yayınlanmaz (başka öğrencilere gösterilmez); yalnızca kalite kontrolünden geçenler döner.
    /// </summary>
    public record GetClipsFromVideoQuery(Guid UserId, string Url) : IRequest<List<LearnerClipDto>>;

    public class GetClipsFromVideoQueryHandler(IWordWiseDbContext _context, ClipGenerator _generator)
        : IRequestHandler<GetClipsFromVideoQuery, List<LearnerClipDto>>
    {
        public async Task<List<LearnerClipDto>> Handle(GetClipsFromVideoQuery request, CancellationToken cancellationToken)
        {
            var youtubeId = ClipGenerator.ExtractYoutubeId(request.Url);
            if (youtubeId is null)
                throw new BusinessException("Geçerli bir YouTube bağlantısı gir (watch?v=, youtu.be veya shorts).");

            var existing = await _context.VideoClips
                .Where(c => c.YoutubeId == youtubeId && !c.IsRejected && c.QcStatus == ClipQcStatus.Passed)
                .OrderBy(c => c.StartSec)
                .ToListAsync(cancellationToken);
            if (existing.Count > 0)
                return existing.Select(c => LearnerClipMapper.ToDto(c, isSaved: false)).ToList();

            var generated = await _generator.GenerateAsync(youtubeId, null, cancellationToken, autoPublish: false);
            var passed = generated.Where(c => c.QcStatus == "Passed").OrderBy(c => c.StartSec).ToList();
            if (passed.Count == 0)
                throw new BusinessException("Bu videodan uygun bir alıştırma cümlesi çıkarılamadı (altyazı yok ya da cümleler uygun değil). Başka bir video dene.");

            return passed.Select(c => new LearnerClipDto
            {
                Id = c.Id,
                YoutubeId = c.YoutubeId,
                StartSec = c.StartSec,
                EndSec = c.EndSec,
                Text = c.Text,
                Words = c.Words,
                HasBurnedInCaptions = c.HasBurnedInCaptions,
                SourceTitle = c.SourceTitle,
            }).ToList();
        }
    }
}
