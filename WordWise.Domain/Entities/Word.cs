using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Domain.Enums;

namespace WordWise.Domain.Entities
{
    public class Word : BaseEntity
    {
        public string Text { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? Ipa { get; set; }
        public string? PartOfSpeech { get; set; }
        public CefrLevel? CefrLevel { get; set; }
        public string? ExampleSentencesJson { get; set; }

        public string? SynonymsJson { get; set; }
        public string? AntonymsJson { get; set; }
        public string? Category { get; set; }
        public string? SourceProvider { get; set; }  
        public string? SourceUrl { get; set; }
        public DateTime? ImportedAt { get; set; }

        public bool IsPublished { get; set; } = false;

        // Navigation properties
        public ICollection<UserWord> UserWords { get; set; } = new List<UserWord>();
        public ICollection<Video> Videos { get; set; } = new List<Video>();
        public ICollection<VideoCandidate> VideoCandidates { get; set; } = new List<VideoCandidate>();
    }
}
