using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Clips.Dtos;

namespace WordWise.Application.Features.Clips.Queries.GetClips
{
    /// <summary>Admin listesi için en son üretilen 100 klip (reddedilenler hariç).</summary>
    public record GetRecentClipsQuery() : IRequest<List<VideoClipDto>>;

    public class GetRecentClipsQueryHandler(IWordWiseDbContext _context)
        : IRequestHandler<GetRecentClipsQuery, List<VideoClipDto>>
    {
        public async Task<List<VideoClipDto>> Handle(GetRecentClipsQuery request, CancellationToken cancellationToken)
        {
            var clips = await _context.VideoClips
                .Where(x => !x.IsRejected)
                .OrderByDescending(x => x.CreatedAt)
                .ThenBy(x => x.StartSec)
                .Take(100)
                .ToListAsync(cancellationToken);

            return clips.Select(VideoClipMapper.ToDto).ToList();
        }
    }
}
