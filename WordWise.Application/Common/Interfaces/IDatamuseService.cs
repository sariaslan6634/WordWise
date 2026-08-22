using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Common.Interfaces
{
    public interface IDatamuseService
    {
        Task<List<string>> GetSynonymsAsync(
            string word,
            CancellationToken cancellationToken = default);

        Task<List<string>> GetAntonymsAsync(
            string word,
            CancellationToken cancellationToken = default);
    }
}
