using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Common.Settings
{
    public class EmailSettings
    {
        public const string SectionName = "EmailSettings";

        public string ApiKey { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string AppBaseUrl { get; set; } = string.Empty;
    }
}
