using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Domain.Entities
{
    public class Video : BaseEntity
    {
        public Guid WordId { get; set; }
        public Word Word { get; set; } = null!;

        public string YoutubeId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? ChannelTitle { get; set; }
        public string? ThumbnailUrl { get; set; }

        public int StartSec { get; set; } = 0;
        public int EndSec { get; set; } = 60;

        public int DisplayOrder { get; set; } = 0;

        public string? Transcript { get; set; }

        public bool IsPublished { get; set; } = false;

        public Guid? SourceCandidateId { get; set; }
        public VideoCandidate? SourceCandidate { get; set; }

        public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
    }
}
