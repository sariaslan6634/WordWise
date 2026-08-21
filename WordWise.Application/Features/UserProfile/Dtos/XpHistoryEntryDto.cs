using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.UserProfile.Dtos
{
    public class XpHistoryEntryDto
    {
        public DateTime Date { get; set; }
        public int XpChange { get; set; }
        public int TotalXp { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
