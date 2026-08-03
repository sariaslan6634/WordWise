using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.UserWords.Dtos
{
    public class SavedWordDto
    {
        public Guid Id { get; set; }
        public Guid WordId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? PartOfSpeech { get; set; }
        public string? CefrLevel { get; set; }
        public int KnownLevel { get; set; }
        public DateTime? NextReviewAt { get; set; }
        public int ReviewCount { get; set; }
        public string? PersonalNote { get; set; }
        public DateTime SavedAt { get; set; }
    }
}
