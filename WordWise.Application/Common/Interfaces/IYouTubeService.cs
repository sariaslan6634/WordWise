using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Application.Features.Videos.Dtos;

namespace WordWise.Application.Common.Interfaces
{
    public interface IYouTubeService
    {
        Task<List<YouTubeCandidateDto>> SearchVideoAsync(string searchQuery,
            int maxResults,
            CancellationToken cancellationToken = default);


    }
}
