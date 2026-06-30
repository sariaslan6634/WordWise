using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Domain.Entities;

namespace WordWise.Application.Common.Interfaces.Repositories
{
    public interface IVideoCandidateRepository : IRepository<VideoCandidate>
    {
        Task<IReadOnlyList<VideoCandidate>> GetPendingByWordIdAsync(
            Guid wordId,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<VideoCandidate> Items, int TotalCount)> GetAllPendingPagedAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(
            Guid wordId,
            string youtubeId,
            CancellationToken cancellationToken = default);
    }
}
