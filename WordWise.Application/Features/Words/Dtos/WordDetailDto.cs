using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.Words.Dtos
{
    public class WordDetailDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? Ipa { get; set; }
        public string? PartOfSpeech { get; set; }
        public string? CefrLevel { get; set; }

        public List<string> ExampleSentences { get; set; } = new();
        public List<string> Synonyms { get; set; } = new();
        public List<string> Antonyms { get; set; } = new();

        public string? Category { get; set; }

        public List<WordVideoDto> Videos { get; set; } = new();
    }
    public class WordVideoDto
    {
        public Guid Id { get; set; }
        public string YoutubeId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? ChannelTitle { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int StartSec { get; set; }
        public int EndSec { get; set; }
        public int DisplayOrder { get; set; }
    }
}
