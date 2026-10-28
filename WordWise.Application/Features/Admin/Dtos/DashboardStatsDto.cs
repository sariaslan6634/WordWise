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
        public int ActiveUsersLast30Days { get; set; }
        public int QuizAnswersLast7Days { get; set; }
        public int WordsAddedLast24Hours { get; set; }
        public int VideosAddedLast24Hours { get; set; }
        public int SessionsOpenedLast24Hours { get; set; }
        public int UsersRegisteredLast24Hours { get; set; }
        public int PendingWordCount { get; set; }
        public List<DailyUserActivityDto> DailyUserActivity { get; set; } = new();

        public int PendingVideoCandidates { get; set; }

        public int TodayCorrectAnswers { get; set; }
        public int TodayWrongAnswers { get; set; }
        public int TodayTotalAnswers { get; set; }

        public List<PopularWordDto> TopSearchedWords { get; set; } = new();
        public List<HardQuizDto> HardestQuestions { get; set; } = new();

        public int TotalXpEarned { get; set; }
    }

    public class DailyUserActivityDto
    {
        public DateTime Date { get; set; }
        public int ActiveUsers { get; set; }
        public int Sessions { get; set; }
    }

    public class PendingContentDto
    {
        public List<PendingWordDto> Words { get; set; } = new();
        public List<PendingVideoCandidateDto> VideoCandidates { get; set; } = new();
    }

    public class PendingWordDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PendingVideoCandidateDto
    {
        public Guid Id { get; set; }
        public Guid WordId { get; set; }
        public string WordText { get; set; } = string.Empty;
        public string YoutubeId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? ChannelTitle { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Description { get; set; }
        public int? DurationSeconds { get; set; }
        public DateTime FetchedAt { get; set; }
    }
}
