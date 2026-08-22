using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.Words.Dtos
{
    public class FreeDictionaryResponseDto
    {
        public string Word { get; set; } = string.Empty;
        public List<FreeDictionaryPhonetic> Phonetics { get; set; } = new();
        public List<FreeDictionaryMeaning> Meanings { get; set; } = new();
    }

    public class FreeDictionaryPhonetic
    {
        public string? Text { get; set; }
    }

    public class FreeDictionaryMeaning
    {
        public string PartOfSpeech { get; set; } = string.Empty;
        public List<FreeDictionaryDefinition> Definitions { get; set; } = new();
    }

    public class FreeDictionaryDefinition
    {
        public string Definition { get; set; } = string.Empty;
        public string? Example { get; set; }
    }
}
