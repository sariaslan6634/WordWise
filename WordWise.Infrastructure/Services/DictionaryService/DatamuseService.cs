using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WordWise.Application.Common.Interfaces;

namespace WordWise.Infrastructure.Services.DictionaryService
{
    public class DatamuseService(
        HttpClient _httpClient,
        ILogger<DatamuseService> _logger) : IDatamuseService
    {
        private const string BaseUrl = "https://api.datamuse.com/words";
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        public async Task<List<string>> GetSynonymsAsync(
            string word,
            CancellationToken cancellationToken = default)
            => await FetchWordsAsync($"{BaseUrl}?rel_syn={Uri.EscapeDataString(word)}&max=5",
                word, "synonyms", cancellationToken);

        public async Task<List<string>> GetAntonymsAsync(
            string word,
            CancellationToken cancellationToken = default)
            => await FetchWordsAsync($"{BaseUrl}?rel_ant={Uri.EscapeDataString(word)}&max=5",
                word, "antonyms", cancellationToken);

        public async Task<List<string>> GetDailyWordSuggestionsAsync(CancellationToken cancellationToken = default)
        {
            var length = 5 + DateTime.UtcNow.DayOfYear % 5;
            var pattern = new string('?', length);
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{BaseUrl}?sp={Uri.EscapeDataString(pattern)}&max=100&md=p", cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Datamuse daily word request failed with {StatusCode}", response.StatusCode);
                    return new List<string>();
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var results = JsonSerializer.Deserialize<List<DatamuseWord>>(json, _jsonOptions) ?? new();
                var candidates = results
                    .Where(item => item.Word.Length == length && item.Word.All(char.IsAsciiLetter))
                    .Where(item => item.Tags is null || item.Tags.Any(tag => tag is "n" or "v" or "adj" or "adv"))
                    .Select(item => item.Word.ToLowerInvariant())
                    .Distinct()
                    .ToList();

                if (candidates.Count == 0) return candidates;
                var offset = DateTime.UtcNow.DayOfYear * 97 % candidates.Count;
                return candidates.Skip(offset).Concat(candidates.Take(offset)).Take(15).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching daily word suggestions from Datamuse");
                return new List<string>();
            }
        }

        private async Task<List<string>> FetchWordsAsync(
            string url,
            string word,
            string type,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Datamuse API error for {Type} of '{Word}'. Status: {Status}",
                        type, word, response.StatusCode);
                    return new List<string>();
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                // Datamuse [{"word":"happy","score":1234}, ...] formatında döner
                var results = JsonSerializer.Deserialize<List<DatamuseWord>>(json, _jsonOptions);

                return results?.Select(x => x.Word).ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching {Type} for word: {Word}", type, word);
                return new List<string>();
            }
        }
    }

    file class DatamuseWord
    {
        public string Word { get; set; } = string.Empty;
        public int Score { get; set; }
        public List<string>? Tags { get; set; }
    }
}
