using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Constants;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Words.Commands.DeleteWord
{
    public record DeleteWordCommand(Guid Id) : IRequest;
    public class DeleteWordCommandHandler(IWordWiseDbContext _context, ICacheService _cache) : IRequestHandler<DeleteWordCommand>
    {
        public async Task Handle(DeleteWordCommand request, CancellationToken cancellationToken)
        {
            var word = await _context.Words.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (word is null)
                throw new NotFoundException(nameof(Word), request.Id);
            word.IsDeleted = true;

            await _context.SaveChangesAsync(cancellationToken);

            await _cache.RemoveAsync(CacheKeys.WordByText(word.Text), cancellationToken);
        }
    }

    public class DeleteWordCommandValidator : AbstractValidator<DeleteWordCommand>
    {
        public DeleteWordCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Word id'si boş olamaz.");
        }
    }
}
