using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.UserWords.Dtos;

namespace WordWise.Application.Features.UserWords.Queries.GetRemovedWords
{
    /// <summary>Kelime defterinden çıkarılıp henüz 24 saati dolmamış kelimeler ("Silinenler").</summary>
    public record GetRemovedWordsQuery(Guid UserId, DateTime? Now = null) : IRequest<List<RemovedWordDto>>;

    public class GetRemovedWordsQueryHandler(IWordWiseDbContext _context) : IRequestHandler<GetRemovedWordsQuery, List<RemovedWordDto>>
    {
        /// <summary>Çıkarılan kelime bu süre boyunca geri alınabilir.</summary>
        public static readonly TimeSpan Retention = TimeSpan.FromHours(24);

        public async Task<List<RemovedWordDto>> Handle(GetRemovedWordsQuery request, CancellationToken cancellationToken)
        {
            var now = request.Now ?? DateTime.UtcNow;
            var cutoff = now - Retention;

            // Silme anı UpdatedAt'e yazılır (UnsaveWord). Süresi dolan satırlar listede görünmez.
            var rows = await _context.UserWords.IgnoreQueryFilters()
                .Include(x => x.Word)
                .Where(x => x.UserId == request.UserId && x.IsDeleted && x.UpdatedAt != null && x.UpdatedAt > cutoff && !x.Word.IsDeleted)
                .OrderBy(x => x.UpdatedAt)
                .ToListAsync(cancellationToken);

            return rows.Select(x =>
            {
                var removedAt = DateTime.SpecifyKind(x.UpdatedAt!.Value, DateTimeKind.Utc);
                return new RemovedWordDto
                {
                    WordId = x.WordId,
                    Text = x.Word.Text,
                    Definition = x.Word.Definition,
                    PartOfSpeech = x.Word.PartOfSpeech,
                    RemovedAt = removedAt,
                    ExpiresAt = removedAt + Retention,
                };
            }).ToList();
        }
    }
}
