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

    public class VideoRepository : Repository<Video>, IVideoRepository
    {
        public VideoRepository(WordWiseDbContext context) : base(context) { }

        public async Task<IReadOnlyList<Video>> GetPublishedByWordIdAsync(
            Guid wordId,
            CancellationToken cancellationToken = default)
            => await _dbSet
                .Where(x => x.WordId == wordId && x.IsPublished)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync(cancellationToken);

        public async Task<Video?> GetWithQuizQuestionsAsync(
            Guid videoId,
            CancellationToken cancellationToken = default)
            => await _dbSet
                .Include(x => x.QuizQuestions.Where(q => !q.IsDeleted && q.IsPublished))
                .FirstOrDefaultAsync(x => x.Id == videoId, cancellationToken);

        public async Task<bool> ExistsAsync(
            Guid wordId,
            string youtubeId,
            CancellationToken cancellationToken = default)
            => await _dbSet
                .AnyAsync(x => x.WordId == wordId && x.YoutubeId == youtubeId, cancellationToken);
    }
}
