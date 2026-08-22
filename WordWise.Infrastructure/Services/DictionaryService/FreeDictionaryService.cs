using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Words.Dtos;

namespace WordWise.Infrastructure.Services.DictionaryService
{
    public class FreeDictionaryService(HttpClient _httpclient,
        ILogger<FreeDictionaryService> _logger) : IFreeDictionaryService
    {
        private const string BaseUrl = "https://api.dictionaryapi.dev/api/v2/entries/en";
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public async Task<FreeDictionaryResponseDto?> GetWordAsync(string word, CancellationToken cancellationToken = default)
        {
            try
            {
                var ulr = $"{BaseUrl}/{Uri.EscapeDataString(word.ToLowerInvariant().Trim())}";
                var response = await _httpclient.GetAsync(ulr, cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Word not found in Free Dictionary API: {Word}", word);
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Free Dictionary API error. Status: {Status}, Word: {Word}",
                        response.StatusCode, word);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                var results = JsonSerializer.Deserialize<List<FreeDictionaryResponseDto>>(
                    json, _jsonOptions);

                return results?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching word: {Word}", word);
                return null;
            }
        }
    }
}
