using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Application.Features.Words.Dtos;

namespace WordWise.Application.Common.Interfaces
{
    public interface IFreeDictionaryService
    {
        Task<FreeDictionaryResponseDto?> GetWordAsync(
            string word,
            CancellationToken cancellationToken = default);
    }
}
