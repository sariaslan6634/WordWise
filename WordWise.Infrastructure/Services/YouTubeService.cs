using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Common.Settings;
using WordWise.Application.Features.Videos.Dtos;

namespace WordWise.Infrastructure.Services
{
    public class YouTubeService(
        HttpClient _httpClient,
        IOptions<YoutubeSettings> _options,
        ILogger<YouTubeService> _logger) : IYouTubeService
    {
        private readonly YoutubeSettings _settings = _options.Value;
        private const string BaseUrl = "https://www.googleapis.com/youtube/v3";
        public async Task<List<YouTubeCandidateDto>> SearchVideoAsync(string searchQuery, int maxResults, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{BaseUrl}/search" +
                          $"?part=snippet" +
                          $"&q={Uri.EscapeDataString(searchQuery)}" +
                          $"&type=video" +
                          $"&maxResults={maxResults}" +
                          $"&relevanceLanguage=en" +
                          $"&key={_settings.ApiKey}";

                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "YouTube API hatası. Status: {Status}, Body: {Body}",
                        response.StatusCode, error);
                    return new List<YouTubeCandidateDto>();
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<YouTubeSearchResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if(result?.Items is null)
                    return new List<YouTubeCandidateDto>();

                return result.Items.Select(item => new YouTubeCandidateDto
                {
                    YoutubeId = item.Id.VideoId,
                    Title = item.Snippet.Title,
                    ChannelTitle = item.Snippet.ChannelTitle,
                    ThumbnailUrl = item.Snippet.Thumbnails?.Medium?.Url,
                    Description = item.Snippet.Description,
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "YouTube'da {Query} için arama yapılırken beklenmeyen bir hata oluştu.", searchQuery);
                return new List<YouTubeCandidateDto>();
            }
        }
    }
    file class YouTubeSearchResponse
    {
        public List<YouTubeSearchItem> Items { get; set; } = new();
    }

    file class YouTubeSearchItem
    {
        public YouTubeSearchItemId Id { get; set; } = new();
        public YouTubeSnippet Snippet { get; set; } = new();
    }

    file class YouTubeSearchItemId
    {
        public string VideoId { get; set; } = string.Empty;
    }

    file class YouTubeSnippet
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ChannelTitle { get; set; }
        public YouTubeThumbnails? Thumbnails { get; set; }
    }

    file class YouTubeThumbnails
    {
        public YouTubeThumbnail? Medium { get; set; }
    }

    file class YouTubeThumbnail
    {
        public string? Url { get; set; }
    }
}
