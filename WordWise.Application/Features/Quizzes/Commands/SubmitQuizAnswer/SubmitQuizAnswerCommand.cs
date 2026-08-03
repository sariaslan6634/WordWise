using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Quizzes.Dtos;
using WordWise.Domain.Entities;
using WordWise.Domain.Enums;

namespace WordWise.Application.Features.Quizzes.Commands.SubmitQuizAnswer
{
    public record SubmitQuizAnswerCommand(
        Guid UserId,
        Guid QuizQuestionId,
        string GivenAnswer
        ) : IRequest<QuizAnswerResultDto>;

    public class SubmitQuizAnswerCommandHandler(IWordWiseDbContext _context) : IRequestHandler<SubmitQuizAnswerCommand, QuizAnswerResultDto>
    {
        private const int XpCorrect = 10;
        private const int XpWrong = -8;
        public async Task<QuizAnswerResultDto> Handle(SubmitQuizAnswerCommand request, CancellationToken cancellationToken)
        {
            var question = await _context.QuizQuestions
                .FirstOrDefaultAsync(x => x.Id == request.QuizQuestionId, cancellationToken);
            if (question is null)
                throw new NotFoundException(nameof(QuizQuestion), request.QuizQuestionId);
            var isCorrect = string.Equals(request.GivenAnswer.Trim(),
               question.CorrectAnswer.Trim(),
               StringComparison.OrdinalIgnoreCase);

            var twentwentyFourHoursAgo = DateTime.UtcNow.AddHours(-24);
            var alreadyAnsweredToday = await _context.UserQuizAnswers.AnyAsync(x => x.UserId == request.UserId && x.QuizQuestionId == request.QuizQuestionId && x.AnsweredAt >= twentwentyFourHoursAgo, cancellationToken);

            var currentXp = await _context.UserXpHistories
                .Where(x => x.UserId == request.UserId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.TotalXpAfterChange)
                .FirstOrDefaultAsync(cancellationToken);

            int xpChange = 0;
            int newTotalXp = currentXp;
            bool xpAwarded = false;

            if (!alreadyAnsweredToday)
            {
                xpChange = isCorrect ? XpCorrect : XpWrong;

                newTotalXp = Math.Max(0, currentXp + xpChange);
                xpChange = newTotalXp - currentXp; 

                // XP geçmişine kaydet
                var xpHistory = new UserXpHistory
                {
                    UserId = request.UserId,
                    XpChange = xpChange,
                    TotalXpAfterChange = newTotalXp,
                    Reason = isCorrect ? XpReason.QuizCorrect : XpReason.QuizWrong,
                };
                await _context.UserXpHistories.AddAsync(xpHistory, cancellationToken);
                xpAwarded = true;
            }
            var answer = new UserQuizAnswer
            {
                UserId = request.UserId,
                QuizQuestionId = request.QuizQuestionId,
                GivenAnswer = request.GivenAnswer.Trim(),
                IsCorrect = isCorrect,
                XpChange = xpChange,
                AnsweredAt = DateTime.UtcNow,
            };
            await _context.UserQuizAnswers.AddAsync(answer, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var message = alreadyAnsweredToday
                ? "Bu soruyu bugün zaten yanıtladınız. XP kazanılmadı."
                : isCorrect
                    ? $"Doğru! {xpChange} XP kazandınız."
                    : $"Yanlış! Doğru cevap '{question.CorrectAnswer}'. {Math.Abs(xpChange)} XP düşüldü.";
            return new QuizAnswerResultDto
            {
                IsCorrect = isCorrect,
                CorrectAnswer = question.CorrectAnswer,
                XpChange = xpChange,
                TotalXp = newTotalXp,
                Message = message,
                XpAwarded = xpAwarded,
            };
        }
    }

    public class SubmitQuizAnswerCommandValidator : AbstractValidator<SubmitQuizAnswerCommand>
    {
        public SubmitQuizAnswerCommandValidator()
        {
            RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı ID zorunludur.");

            RuleFor(x => x.QuizQuestionId)
                .NotEmpty().WithMessage("Test soru ID zorunludur.");

            RuleFor(x => x.GivenAnswer)
                .NotEmpty().WithMessage("Cevap zorunludur.")
                .MaximumLength(500).WithMessage("Cevap en fazla 500 karakter olabilir.");
        }
    }
}
