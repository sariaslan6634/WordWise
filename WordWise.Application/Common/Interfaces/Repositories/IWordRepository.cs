using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Domain.Entities;

namespace WordWise.Application.Common.Interfaces.Repositories
{
    public interface IWordRepository : IRepository<Word>
    {
        Task<Word?> GetByTextAsync(string text, CancellationToken cancellationToken = default);

        Task<Word?> GetPublishedByTextAsync(string text, CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<Word> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            string? searchText = null,
            CancellationToken cancellationToken = default);

        Task<Word?> GetWithVideosAsync(Guid wordId, CancellationToken cancellationToken = default);
    }
}
