using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.UserProfile.Dtos
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime MemberSince { get; set; }

        public int TotalXp { get; set; }

        public int SavedWordCount { get; set; }
        public int MasteredWordCount { get; set; } 
        public int PendingReviewCount { get; set; }

        public int TotalQuizAnswers { get; set; }
        public int CorrectAnswers { get; set; }
        public double QuizAccuracy { get; set; }

        public List<XpHistoryEntryDto> RecentXpHistory { get; set; } = new();
    }
}
