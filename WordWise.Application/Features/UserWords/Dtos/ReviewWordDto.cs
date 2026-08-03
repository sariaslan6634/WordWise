using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.UserWords.Dtos
{
    public class ReviewWordDto
    {
        public Guid UserWordId { get; set; }
        public Guid WordId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? Ipa { get; set; }
        public List<string> ExampleSentences { get; set; } = new();
        public int KnownLevel { get; set; }
        public DateTime? NextReviewAt { get; set; }
    }
}
