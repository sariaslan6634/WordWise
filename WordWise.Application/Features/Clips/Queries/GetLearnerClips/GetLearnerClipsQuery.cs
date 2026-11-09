using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Clips.Dtos;
using WordWise.Application.Features.Clips.Models;
using WordWise.Application.Features.Clips.Services;

namespace WordWise.Application.Features.Clips.Queries.GetLearnerClips
{
    /// <summary>
    /// Yayındaki kliplerden rastgele bir seçki; kullanıcının daha önce bildirdiği klipler hariç.
    /// <paramref name="Categories"/> boş/null ise tüm klipler, doluysa sadece o kategoriler gelir.
    /// </summary>
    public record GetLearnerClipsQuery(Guid UserId, int Count, IReadOnlyCollection<ClipCategory>? Categories = null) : IRequest<List<LearnerClipDto>>;

    public class GetLearnerClipsQueryHandler(IWordWiseDbContext _context)
        : IRequestHandler<GetLearnerClipsQuery, List<LearnerClipDto>>
    {
        public async Task<List<LearnerClipDto>> Handle(GetLearnerClipsQuery request, CancellationToken cancellationToken)
        {
            var count = Math.Clamp(request.Count, 1, 50);
            var reportedByUser = _context.ClipReports
                .Where(r => r.UserId == request.UserId)
                .Select(r => r.ClipId);

            // Önce hafif sütunlarla aday listesi; kategori kurallı olduğu için bellekte süzülür.
            var candidates = await _context.VideoClips
                .Where(c => c.IsPublished && !c.IsRejected && !reportedByUser.Contains(c.Id))
                .Select(c => new { c.Id, c.Text, c.SourceTitle })
                .ToListAsync(cancellationToken);

            var wanted = request.Categories is { Count: > 0 } ? request.Categories : null;
            var categoryOf = new Dictionary<Guid, ClipCategory>();
            var ids = new List<Guid>();
            foreach (var c in candidates.OrderBy(_ => Random.Shared.Next()))
            {
                var category = ClipClassifier.Classify(c.Text, c.SourceTitle);
                if (wanted is not null && !wanted.Contains(category)) continue;
                categoryOf[c.Id] = category;
                ids.Add(c.Id);
                if (ids.Count == count) break;
            }

            var clips = await _context.VideoClips.Where(c => ids.Contains(c.Id)).ToListAsync(cancellationToken);
            var byId = clips.ToDictionary(c => c.Id);

            return ids.Where(byId.ContainsKey).Select(id =>
            {
                var c = byId[id];
                return new LearnerClipDto
                {
                    Id = c.Id,
                    YoutubeId = c.YoutubeId,
                    StartSec = c.StartSec,
                    EndSec = c.EndSec,
                    Text = c.Text,
                    HasBurnedInCaptions = c.HasBurnedInCaptions,
                    Category = ClipClassifier.Key(categoryOf[id]),
                    Words = JsonSerializer.Deserialize<List<CaptionWord>>(c.WordTimingsJson, VideoClipMapper.JsonOptions) ?? new(),
                };
            }).ToList();
        }
    }
}
