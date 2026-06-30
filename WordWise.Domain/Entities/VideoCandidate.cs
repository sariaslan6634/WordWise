using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Domain.Entities
{
    public class VideoCandidate : BaseEntity
    {
        public Guid WordId { get; set; }
        public Word Word { get; set; } = null!;

        public string YoutubeId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? ChannelTitle { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Description { get; set; }
        public int? DurationSeconds { get; set; }

        public DateTime FetchedAt { get; set; } = DateTime.UtcNow;

        public bool IsApproved { get; set; } = false;
        public bool IsRejected { get; set; } = false;

        public Guid? ApprovedByUserId { get; set; }
        public User? ApprovedByUser { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}
