using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.UserWords.Commands.ReviewWord
{
    /// <summary>Tekrar sırasında bir kelime için "bildim / bilemedim" cevabını işler ve bir sonraki tekrar zamanını belirler.</summary>
    public record ReviewWordCommand(Guid UserId, Guid WordId, bool Known) : IRequest<ReviewWordResult>;

    public record ReviewWordResult(int KnownLevel, DateTime NextReviewAt);

    public class ReviewWordCommandHandler(IWordWiseDbContext _context)
        : IRequestHandler<ReviewWordCommand, ReviewWordResult>
    {
        public const int MaxLevel = 5;

        /// <summary>Leitner benzeri aralıklar: yeni seviyeye göre bir sonraki tekrara kadar gün.</summary>
        private static readonly int[] IntervalDays = { 1, 2, 4, 8, 16, 32 };

        public static (int Level, DateTime Next) Schedule(int level, bool known, DateTime now)
        {
            if (!known) return (Math.Max(0, level - 1), now.AddMinutes(10)); // bilemedi: kısa süre sonra tekrar
            var next = Math.Min(MaxLevel, level + 1);
            return (next, now.AddDays(IntervalDays[next]));
        }

        public async Task<ReviewWordResult> Handle(ReviewWordCommand request, CancellationToken cancellationToken)
        {
            var userWord = await _context.UserWords
                .FirstOrDefaultAsync(x => x.UserId == request.UserId && x.WordId == request.WordId, cancellationToken);
            if (userWord is null)
                throw new NotFoundException(nameof(UserWord), request.WordId);

            var now = DateTime.UtcNow;
            var (level, next) = Schedule(userWord.KnownLevel, request.Known, now);
            userWord.KnownLevel = level;
            userWord.NextReviewAt = next;
            userWord.ReviewCount += 1;
            userWord.UpdatedAt = now;
            await _context.SaveChangesAsync(cancellationToken);

            return new ReviewWordResult(level, next);
        }
    }

    public class ReviewWordCommandValidator : AbstractValidator<ReviewWordCommand>
    {
        public ReviewWordCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.WordId).NotEmpty();
        }
    }
}
