using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Admin.Dtos;

namespace WordWise.Application.Features.Admin.Queries.GetDashboardStats
{
    public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;
    public class GetDashboardStatsQueryHandler(IWordWiseDbContext _context) : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
    {
        public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var sevenDaysAgo = now.AddDays(-7);
            var thirtyDaysAgo = now.Date.AddDays(-29);
            var last24Hours = now.AddHours(-24);
            var todayStart = now.Date;

            var totalWords = await _context.Words.CountAsync(cancellationToken);

            var totalVideos = await _context.Videos
                .CountAsync(cancellationToken);

            var totalQuizQuestions = await _context.QuizQuestions
                .CountAsync(cancellationToken);

            var totalUsers = await _context.Users
                .CountAsync(cancellationToken);

            var newUsersToday = await _context.Users
                .CountAsync(x => x.CreatedAt >= todayStart, cancellationToken);

            var activeUsersLast7Days = await _context.UserQuizAnswers
                .Where(x => x.AnsweredAt >= sevenDaysAgo)
                .Select(x => x.UserId)
                .Distinct()
                .CountAsync(cancellationToken);

            var activeUsersLast30Days = await _context.UserLoginEvents
                .Where(x => x.LoggedInAt >= thirtyDaysAgo)
                .Select(x => x.UserId)
                .Distinct()
                .CountAsync(cancellationToken);

            var dailyUserActivity = await _context.UserLoginEvents
                .Where(x => x.LoggedInAt >= thirtyDaysAgo)
                .GroupBy(x => x.LoggedInAt.Date)
                .Select(g => new DailyUserActivityDto
                {
                    Date = g.Key,
                    ActiveUsers = g.Select(x => x.UserId).Distinct().Count(),
                    Sessions = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);

            var wordsAddedLast24Hours = await _context.Words
                .CountAsync(x => x.CreatedAt >= last24Hours, cancellationToken);
            var videosAddedLast24Hours = await _context.Videos
                .CountAsync(x => x.CreatedAt >= last24Hours, cancellationToken);
            var sessionsOpenedLast24Hours = await _context.UserLoginEvents
                .CountAsync(x => x.LoggedInAt >= last24Hours, cancellationToken);
            var usersRegisteredLast24Hours = await _context.Users
                .CountAsync(x => x.CreatedAt >= last24Hours, cancellationToken);
            var pendingWordCount = await _context.Words
                .CountAsync(x => !x.IsApproved, cancellationToken);

            var quizAnswersLast7Days = await _context.UserQuizAnswers
               .CountAsync(x => x.AnsweredAt >= sevenDaysAgo, cancellationToken);

            var pendingVideoCandidates = await _context.VideoCandidates
                .CountAsync(x => !x.IsApproved && !x.IsRejected, cancellationToken);

            var todayAnswers = await _context.UserQuizAnswers
                .Where(x => x.AnsweredAt >= todayStart)
                .Select(x => new { x.IsCorrect })
                .ToListAsync(cancellationToken);

            var todayTotalAnswers = todayAnswers.Count;
            var todayCorrectAnswers = todayAnswers.Count(x => x.IsCorrect);
            var todayWrongAnswers = todayTotalAnswers - todayCorrectAnswers;

            var topSearchedWords = await _context.UserWords
                .Include(x => x.Word)
                .GroupBy(x => new { x.WordId, x.Word.Text })
                .Select(g => new PopularWordDto
                {
                    Text = g.Key.Text,
                    SaveCount = g.Count()
                })
                .OrderByDescending(x => x.SaveCount)
                .Take(5)
                .ToListAsync(cancellationToken);
            var hardestQuestions = await _context.UserQuizAnswers
                .GroupBy(x => new { x.QuizQuestionId, x.QuizQuestion.QuestionText })
                .Select(g => new HardQuizDto
                {
                    QuizQuestionId = g.Key.QuizQuestionId,
                    QuestionText = g.Key.QuestionText,
                    TotalAnswers = g.Count(),
                    WrongAnswers = g.Count(x => !x.IsCorrect),
                    WrongRate = Math.Round(
                        (double)g.Count(x => !x.IsCorrect) / g.Count() * 100, 1)
                })
                .Where(x => x.TotalAnswers >= 3)
                .OrderByDescending(x => x.WrongRate)
                .Take(5)
                .ToListAsync(cancellationToken);

            var totalXpEarned = await _context.UserXpHistories
                .Where(x => x.XpChange > 0)
                .SumAsync(x => x.XpChange, cancellationToken);

            return new DashboardStatsDto
            {
                TotalWords = totalWords,
                TotalVideos = totalVideos,
                TotalQuizQuestions = totalQuizQuestions,
                TotalUsers = totalUsers,
                NewUsersToday = newUsersToday,
                ActiveUsersLast7Days = activeUsersLast7Days,
                ActiveUsersLast30Days = activeUsersLast30Days,
                QuizAnswersLast7Days = quizAnswersLast7Days,
                WordsAddedLast24Hours = wordsAddedLast24Hours,
                VideosAddedLast24Hours = videosAddedLast24Hours,
                SessionsOpenedLast24Hours = sessionsOpenedLast24Hours,
                UsersRegisteredLast24Hours = usersRegisteredLast24Hours,
                PendingWordCount = pendingWordCount,
                DailyUserActivity = dailyUserActivity,
                PendingVideoCandidates = pendingVideoCandidates,
                TodayTotalAnswers = todayTotalAnswers,
                TodayCorrectAnswers = todayCorrectAnswers,
                TodayWrongAnswers = todayWrongAnswers,
                TopSearchedWords = topSearchedWords,
                HardestQuestions = hardestQuestions,
                TotalXpEarned = totalXpEarned,
            };
        }
    }
}
