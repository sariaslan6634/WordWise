using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Domain.Enums;

namespace WordWise.Application.Features.Words.Dtos
{
    public class CreateWordDto
    {
        public string Text { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? Ipa { get; set; }
        public string? PartOfSpeech { get; set; }
        public CefrLevel? CefrLevel { get; set; }
        public List<string> ExampleSentences { get; set; } = new();
        public List<string> Synonyms { get; set; } = new();
        public List<string> Antonyms { get; set; } = new();
        public string? Category { get; set; }
        public bool IsPublished { get; set; } = false;
    }
}
