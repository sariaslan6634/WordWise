using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.Words.Dtos
{
    public class WordSummaryDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? PartOfSpeech { get; set; }
        public string? CefrLevel { get; set; }
        public string? Category { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
