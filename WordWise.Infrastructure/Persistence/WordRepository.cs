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

    public class WordRepository : Repository<Word>, IWordRepository
    {
        public WordRepository(WordWiseDbContext context) : base(context) { }

        public async Task<Word?> GetByTextAsync(string text, CancellationToken cancellationToken = default)
            => await _dbSet
                .FirstOrDefaultAsync(x => x.Text.ToLower() == text.ToLower(), cancellationToken);

        public async Task<Word?> GetPublishedByTextAsync(string text, CancellationToken cancellationToken = default)
            => await _dbSet
                .Where(x => x.IsPublished)
                .FirstOrDefaultAsync(x => x.Text.ToLower() == text.ToLower(), cancellationToken);

        public async Task<(IReadOnlyList<Word> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            string? searchText = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
                query = query.Where(x => x.Text.Contains(searchText));

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(x => x.Text)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<Word?> GetWithVideosAsync(Guid wordId, CancellationToken cancellationToken = default)
            => await _dbSet
                .Include(x => x.Videos.Where(v => !v.IsDeleted && v.IsPublished))
                .Include(x => x.VideoCandidates.Where(vc => !vc.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == wordId, cancellationToken);
    }
}
