using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Admin.Dtos;

namespace WordWise.Application.Features.Admin.Queries.GetPendingContent
{
    public record GetPendingContentQuery : IRequest<PendingContentDto>;

    public class GetPendingContentQueryHandler(IWordWiseDbContext context)
        : IRequestHandler<GetPendingContentQuery, PendingContentDto>
    {
        public async Task<PendingContentDto> Handle(GetPendingContentQuery request, CancellationToken cancellationToken)
        {
            var words = await context.Words
                .Where(word => !word.IsApproved)
                .OrderBy(word => word.CreatedAt)
                .Select(word => new PendingWordDto
                {
                    Id = word.Id,
                    Text = word.Text,
                    Definition = word.Definition,
                    CreatedAt = word.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var candidates = await context.VideoCandidates
                .Where(candidate => !candidate.IsApproved && !candidate.IsRejected)
                .OrderByDescending(candidate => candidate.FetchedAt)
                .Select(candidate => new PendingVideoCandidateDto
                {
                    Id = candidate.Id,
                    WordId = candidate.WordId,
                    WordText = candidate.Word.Text,
                    YoutubeId = candidate.YoutubeId,
                    Title = candidate.Title,
                    ChannelTitle = candidate.ChannelTitle,
                    ThumbnailUrl = candidate.ThumbnailUrl,
                    Description = candidate.Description,
                    DurationSeconds = candidate.DurationSeconds,
                    FetchedAt = candidate.FetchedAt
                })
                .ToListAsync(cancellationToken);

            return new PendingContentDto { Words = words, VideoCandidates = candidates };
        }
    }
}
