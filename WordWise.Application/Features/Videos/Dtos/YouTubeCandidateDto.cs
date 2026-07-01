using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.Videos.Dtos
{
    public class YouTubeCandidateDto
    {
        public string YoutubeId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? ChannelTitle { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Description { get; set; }
        public int? DurationSeconds { get; set; }
    }
}
