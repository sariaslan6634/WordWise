using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Clips.Dtos;

namespace WordWise.Application.Features.Clips.Queries.GetClips
{
    /// <summary>Admin listesi: onay bekleyen ve yayındaki klipler (reddedilenler hariç), her grup için en son 500.</summary>
    public record GetRecentClipsQuery() : IRequest<List<VideoClipDto>>;

    public class GetRecentClipsQueryHandler(IWordWiseDbContext _context)
        : IRequestHandler<GetRecentClipsQuery, List<VideoClipDto>>
    {
        private const int MaxPerGroup = 500;

        public async Task<List<VideoClipDto>> Handle(GetRecentClipsQuery request, CancellationToken cancellationToken)
        {
            // Eskiden tek listede son 100 klip vardı; yeni üretilenler eski yayındaki klipleri listeden itiyordu (silinmiyordu, sadece görünmüyordu).
            // Artık onay bekleyenler ve yayındakiler ayrı ayrı (her biri en son 500) getirilir.
            var pending = await _context.VideoClips
                .Where(x => !x.IsRejected && !x.IsPublished)
                .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.StartSec)
                .Take(MaxPerGroup)
                .ToListAsync(cancellationToken);
            var published = await _context.VideoClips
                .Where(x => !x.IsRejected && x.IsPublished)
                .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.StartSec)
                .Take(MaxPerGroup)
                .ToListAsync(cancellationToken);
            var clips = pending.Concat(published).ToList();

            return clips.Select(VideoClipMapper.ToDto).ToList();
        }
    }
}
