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

namespace WordWise.Application.Features.UserWords.Commands.UnsaveWord
{
    public record UnsaveWordCommand(Guid UserId, Guid WordId) : IRequest;

    public class UnsaveWordCommandHandler(IWordWiseDbContext _context) : IRequestHandler<UnsaveWordCommand>
    {
        public async Task Handle(UnsaveWordCommand request, CancellationToken cancellationToken)
        {
            var userWord = await _context.UserWords.FirstOrDefaultAsync(x => x.UserId == request.UserId && x.WordId == request.WordId, cancellationToken);

            if (userWord is null)
                throw new NotFoundException(nameof(userWord), request.WordId);

            userWord.IsDeleted = true;
            
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public class UnsaveWordCommandValidator : AbstractValidator<UnsaveWordCommand>
    {
        public UnsaveWordCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId zorunludur.");

            RuleFor(x => x.WordId)
                .NotEmpty().WithMessage("WordId zorunludur.");
        }
    }
}
