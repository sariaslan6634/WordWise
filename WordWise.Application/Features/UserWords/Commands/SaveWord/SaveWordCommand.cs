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
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.UserWords.Commands.SaveWord
{
    public record SaveWordCommand(Guid UserId,Guid WordId) : IRequest<Guid>;

    public class SaveWordCommandHandler(IWordWiseDbContext _context) : IRequestHandler<SaveWordCommand, Guid>
    {
        public async Task<Guid> Handle(SaveWordCommand request, CancellationToken cancellationToken)
        {
            var wordExists = await _context.Words.AnyAsync(x => x.Id == request.WordId && x.IsPublished, cancellationToken);
            if (!wordExists)
                throw new NotFoundException(nameof(Word), request.WordId);

            // Silinmiş (kayıttan çıkarılmış) satırlar da dahil bakılır: (UserId, WordId) benzersiz olduğu için yeniden kaydetmede yeni satır eklenemez, eskisi geri getirilir.
            var existing = await _context.UserWords.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.UserId == request.UserId && x.WordId == request.WordId, cancellationToken);
            if (existing is not null)
            {
                if (!existing.IsDeleted)
                    throw new ConflictException("UserWord", "Bu kelime zaten kaydedilmiş.");

                existing.IsDeleted = false;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.NextReviewAt ??= DateTime.UtcNow.AddDays(1);
                await _context.SaveChangesAsync(cancellationToken);
                return existing.Id;
            }

            var userWord = new UserWord
            {
                UserId = request.UserId,
                WordId = request.WordId,
                KnownLevel = 0,
                ReviewCount = 0,
                NextReviewAt = DateTime.UtcNow.AddDays(1),
            };

            await _context.UserWords.AddAsync(userWord, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return userWord.Id;
        }
    }
    public class SaveWordCommandValidator : AbstractValidator<SaveWordCommand>
    {
        public SaveWordCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId boş olamaz.");
            RuleFor(x => x.WordId).NotEmpty().WithMessage("WordId boş olamaz.");
        }
    }
}
