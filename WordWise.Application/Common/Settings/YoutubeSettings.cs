using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Common.Settings
{
    public class YoutubeSettings
    {
        public const string SectionName = "YouTubeSettings";

        public string ApiKey { get; set; } = string.Empty;
        public int MaxCandidateCount { get; set; } = 10;
    }
}
