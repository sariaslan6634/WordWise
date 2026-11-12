using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Clips.Dtos;

namespace WordWise.Application.Features.Clips.Queries.GetSavedClips
{
    /// <summary>Kullanıcının kaydettiği klipler, en son kaydedilen en üstte.</summary>
    public record GetSavedClipsQuery(Guid UserId) : IRequest<List<LearnerClipDto>>;

    public class GetSavedClipsQueryHandler(IWordWiseDbContext _context)
        : IRequestHandler<GetSavedClipsQuery, List<LearnerClipDto>>
    {
        public async Task<List<LearnerClipDto>> Handle(GetSavedClipsQuery request, CancellationToken cancellationToken)
        {
            var rows = await _context.UserClips
                .Include(u => u.Clip)
                .Where(u => u.UserId == request.UserId && !u.Clip.IsRejected && !u.Clip.IsDeleted)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync(cancellationToken);

            return rows.Select(u => LearnerClipMapper.ToDto(u.Clip, true, DateTime.SpecifyKind(u.CreatedAt, DateTimeKind.Utc))).ToList();
        }
    }
}
