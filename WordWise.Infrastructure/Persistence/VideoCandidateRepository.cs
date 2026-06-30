using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Application.Common.Interfaces.Repositories;
using WordWise.Domain.Entities;
using WordWise.Infrastructure.Persistence.Context;

namespace WordWise.Infrastructure.Persistence
{

    public class VideoCandidateRepository : Repository<VideoCandidate>, IVideoCandidateRepository
    {
        public VideoCandidateRepository(WordWiseDbContext context) : base(context) { }

        public async Task<IReadOnlyList<VideoCandidate>> GetPendingByWordIdAsync(
            Guid wordId,
            CancellationToken cancellationToken = default)
            => await _dbSet
                .Where(x => x.WordId == wordId && !x.IsApproved && !x.IsRejected)
                .OrderByDescending(x => x.FetchedAt)
                .ToListAsync(cancellationToken);

        public async Task<(IReadOnlyList<VideoCandidate> Items, int TotalCount)> GetAllPendingPagedAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Where(x => !x.IsApproved && !x.IsRejected)
                .Include(x => x.Word)
                .OrderByDescending(x => x.FetchedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<bool> ExistsAsync(
            Guid wordId,
            string youtubeId,
            CancellationToken cancellationToken = default)
            => await _dbSet
                .AnyAsync(x => x.WordId == wordId && x.YoutubeId == youtubeId, cancellationToken);
    }
}
