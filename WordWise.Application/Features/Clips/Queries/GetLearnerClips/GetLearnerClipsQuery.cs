using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Clips.Dtos;

namespace WordWise.Application.Features.Clips.Queries.GetLearnerClips
{
    /// <summary>
    /// Rastgele bir klip seçkisi. Varsayılan: yayındaki klipler (kullanıcının bildirdikleri hariç).
    /// SavedOnly: yalnızca kullanıcının kaydettiği klipler.
    /// </summary>
    public record GetLearnerClipsQuery(Guid UserId, int Count, bool SavedOnly = false) : IRequest<List<LearnerClipDto>>;

    public class GetLearnerClipsQueryHandler(IWordWiseDbContext _context)
        : IRequestHandler<GetLearnerClipsQuery, List<LearnerClipDto>>
    {
        public async Task<List<LearnerClipDto>> Handle(GetLearnerClipsQuery request, CancellationToken cancellationToken)
        {
            var count = Math.Clamp(request.Count, 1, 50);
            var savedIds = await _context.UserClips
                .Where(u => u.UserId == request.UserId)
                .Select(u => u.ClipId)
                .ToListAsync(cancellationToken);

            IQueryable<Domain.Entities.VideoClip> query;
            if (request.SavedOnly)
            {
                query = _context.VideoClips.Where(c => !c.IsRejected && savedIds.Contains(c.Id));
            }
            else
            {
                var reportedByUser = _context.ClipReports
                    .Where(r => r.UserId == request.UserId)
                    .Select(r => r.ClipId);
                query = _context.VideoClips.Where(c => c.IsPublished && !c.IsRejected && !reportedByUser.Contains(c.Id));
            }

            var clips = await query.OrderBy(c => Guid.NewGuid()).Take(count).ToListAsync(cancellationToken);
            var saved = savedIds.ToHashSet();
            return clips.Select(c => LearnerClipMapper.ToDto(c, saved.Contains(c.Id))).ToList();
        }
    }
}
