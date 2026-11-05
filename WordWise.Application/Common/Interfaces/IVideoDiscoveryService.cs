using WordWise.Application.Features.Clips.Models;

namespace WordWise.Application.Common.Interfaces
{
    public interface IVideoDiscoveryService
    {
        /// <summary>Altyazılı, gömülebilir, güvenli arama (strict) ile YouTube'da video arar.</summary>
        Task<List<DiscoveredVideoInfo>> SearchAsync(
            string query, int maxResults, string videoDuration, CancellationToken cancellationToken = default);
    }
}
