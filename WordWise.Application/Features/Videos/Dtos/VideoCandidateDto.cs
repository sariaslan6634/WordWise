using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.Videos.Dtos
{
    public class VideoCandidateDto
    {
        public Guid Id { get; set; }
        public Guid WordId { get; set; }
        public string WordText { get; set; } = string.Empty;
        public string YoutubeId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? ChannelTitle { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Description { get; set; }
        public int? DurationSeconds { get; set; }
        public bool IsApproved { get; set; }
        public bool IsRejected { get; set; }
        public DateTime FetchedAt { get; set; }
        public string EmbedUrl => $"https://www.youtube.com/embed/{YoutubeId}";
        public string ThumbnailOrDefault => ThumbnailUrl
            ?? $"https://img.youtube.com/vi/{YoutubeId}/hqdefault.jpg";
    }
    public class ApproveVideoCandidateDto
    {
        public int StartSec { get; set; } = 0;
        public int EndSec { get; set; } = 60;
        public string? Transcript { get; set; }
    }
}
