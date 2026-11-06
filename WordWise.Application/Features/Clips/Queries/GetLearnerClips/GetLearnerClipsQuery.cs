using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Clips.Dtos;
using WordWise.Application.Features.Clips.Models;

namespace WordWise.Application.Features.Clips.Queries.GetLearnerClips
{
    /// <summary>Yayındaki kliplerden rastgele bir seçki; kullanıcının daha önce bildirdiği klipler hariç.</summary>
    public record GetLearnerClipsQuery(Guid UserId, int Count) : IRequest<List<LearnerClipDto>>;

    public class GetLearnerClipsQueryHandler(IWordWiseDbContext _context)
        : IRequestHandler<GetLearnerClipsQuery, List<LearnerClipDto>>
    {
        public async Task<List<LearnerClipDto>> Handle(GetLearnerClipsQuery request, CancellationToken cancellationToken)
        {
            var count = Math.Clamp(request.Count, 1, 50);
            var reportedByUser = _context.ClipReports
                .Where(r => r.UserId == request.UserId)
                .Select(r => r.ClipId);

            var clips = await _context.VideoClips
                .Where(c => c.IsPublished && !c.IsRejected && !reportedByUser.Contains(c.Id))
                .OrderBy(c => Guid.NewGuid())
                .Take(count)
                .ToListAsync(cancellationToken);

            return clips.Select(c => new LearnerClipDto
            {
                Id = c.Id,
                YoutubeId = c.YoutubeId,
                StartSec = c.StartSec,
                EndSec = c.EndSec,
                Text = c.Text,
                HasBurnedInCaptions = c.HasBurnedInCaptions,
                Words = JsonSerializer.Deserialize<List<CaptionWord>>(c.WordTimingsJson, VideoClipMapper.JsonOptions) ?? new(),
            }).ToList();
        }
    }
}
