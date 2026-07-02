using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Common.Settings;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Videos.Commands.FetchVideoCandidates
{
    public record FetchVideoCandidatesCommand(Guid WordId, string WordText
        ) : IRequest<int>;
    public class FetchVideoCandidatesCommandHanld(IWordWiseDbContext _context,
        IYouTubeService _youTubeService,
        IOptions<YoutubeSettings> _youTubeSettings
        ) : IRequestHandler<FetchVideoCandidatesCommand, int>
    {
        public async Task<int> Handle(FetchVideoCandidatesCommand request, CancellationToken cancellationToken)
        {
            var wordExists = await _context.Words.AnyAsync(x => x.Id == request.WordId, cancellationToken);
            if (!wordExists)
                throw new NotFoundException(nameof(Word), request.WordId);

            var searchQuery = $"{request.WordText} english pronunciation example";
            var candidates = await _youTubeService.SearchVideoAsync(searchQuery, _youTubeSettings.Value.MaxCandidateCount, cancellationToken);

            if(!candidates.Any())
                return 0;

            int savedCount = 0;
            foreach (var candidate in candidates)
            {
                var alreadyExists = await _context.VideoCandidates
                    .AnyAsync(x =>
                        x.WordId == request.WordId &&
                        x.YoutubeId == candidate.YoutubeId,
                        cancellationToken);

                if (alreadyExists)
                    continue;

                var videoCandidate = new VideoCandidate
                {
                    WordId = request.WordId,
                    YoutubeId = candidate.YoutubeId,
                    Title = candidate.Title,
                    ChannelTitle = candidate.ChannelTitle,
                    ThumbnailUrl = candidate.ThumbnailUrl,
                    Description = candidate.Description,
                    DurationSeconds = candidate.DurationSeconds,
                    FetchedAt = DateTime.UtcNow,
                    IsApproved = false,
                    IsRejected = false,
                };

                await _context.VideoCandidates.AddAsync(videoCandidate, cancellationToken);
                savedCount++;
            }
            await _context.SaveChangesAsync(cancellationToken);
            return savedCount;
        }
    }
}
