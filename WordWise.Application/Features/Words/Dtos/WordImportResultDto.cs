using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.Words.Dtos
{
    public class WordImportResultDto
    {
        public int TotalRequested { get; set; }
        public int SuccessCount { get; set; }
        public int SkippedCount { get; set; }    
        public int FailedCount { get; set; } 
        public List<string> SkippedWords { get; set; } = new();
        public List<string> FailedWords { get; set; } = new();
    }
}
