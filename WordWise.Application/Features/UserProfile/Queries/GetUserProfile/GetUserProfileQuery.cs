using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.UserProfile.Dtos;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.UserProfile.Queries.GetUserProfile
{
    public record GetUserProfileQuery(Guid UserId) : IRequest<UserProfileDto>;
    public class GetUserProfileQueryHandler(IWordWiseDbContext _context) : IRequestHandler<GetUserProfileQuery, UserProfileDto>
    {
        public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user is null)
                throw new NotFoundException(nameof(User), request.UserId);

            var xpHistory = await _context.UserXpHistories.Where(x => x.UserId == request.UserId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(50)
                .ToListAsync(cancellationToken);

            var userWords = await _context.UserWords.Where(x => x.UserId == request.UserId)
                .Select(x => new { x.KnownLevel, x.NextReviewAt })
                .ToListAsync(cancellationToken);

            var quizAnswers = await _context.UserQuizAnswers.Where(x=>x.UserId == request.UserId)
                .Select(x=> new {x.IsCorrect})
                .ToListAsync(cancellationToken);

            var totalXp = xpHistory.FirstOrDefault()?.TotalXpAfterChange ?? 0;

            var now = DateTime.UtcNow;
            var savedWordCount = userWords.Count;
            var masteredWordCount = userWords.Count(x => x.KnownLevel == 5);  
            var pendingReviewCount = userWords.Count(x => x.NextReviewAt <= now);

            var totalQuizAnswers = quizAnswers.Count;
            var correctAnswers = quizAnswers.Count(x => x.IsCorrect);
            var quizAccuracy = totalQuizAnswers == 0
                ? 0
                : Math.Round((double)correctAnswers / totalQuizAnswers * 100, 1);
            var sevenDaysAgo = now.AddDays(-7);
            var recentXpHistory = xpHistory
                .Where(x => x.CreatedAt >= sevenDaysAgo)
                .Select(x => new XpHistoryEntryDto
                {
                    Date = x.CreatedAt,
                    XpChange = x.XpChange,
                    TotalXp = x.TotalXpAfterChange,
                    Reason = x.Reason.ToString(),
                }).ToList();

            return new UserProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                MemberSince = user.CreatedAt,
                TotalXp = totalXp,
                SavedWordCount = savedWordCount,
                MasteredWordCount = masteredWordCount,
                PendingReviewCount = pendingReviewCount,
                TotalQuizAnswers = totalQuizAnswers,
                CorrectAnswers = correctAnswers,
                QuizAccuracy = quizAccuracy,
                RecentXpHistory = recentXpHistory,
            };
        }
    }
}
