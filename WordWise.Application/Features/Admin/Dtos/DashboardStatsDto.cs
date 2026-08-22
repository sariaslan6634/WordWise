using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Features.Admin.Dtos
{
    public class DashboardStatsDto
    {
        public int TotalWords { get; set; }
        public int TotalVideos { get; set; }
        public int TotalQuizQuestions { get; set; }
        public int TotalUsers { get; set; }
        public int NewUsersToday { get; set; }

        public int ActiveUsersLast7Days { get; set; }
        public int QuizAnswersLast7Days { get; set; }

        public int PendingVideoCandidates { get; set; }

        public int TodayCorrectAnswers { get; set; }
        public int TodayWrongAnswers { get; set; }
        public int TodayTotalAnswers { get; set; }

        public List<PopularWordDto> TopSearchedWords { get; set; } = new();
        public List<HardQuizDto> HardestQuestions { get; set; } = new();

        public int TotalXpEarned { get; set; }
    }
}
