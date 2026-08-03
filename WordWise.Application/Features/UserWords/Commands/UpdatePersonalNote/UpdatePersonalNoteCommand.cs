using FluentValidation;
using FluentValidation.Validators;
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

namespace WordWise.Application.Features.UserWords.Commands.UpdatePersonalNote
{
    public record UpdatePersonalNoteCommand(Guid UserId,Guid WordId, string? PersonalNote) : IRequest;

    public class UpdatePersonalNoteCommandHandler(IWordWiseDbContext _context) : IRequestHandler<UpdatePersonalNoteCommand>
    {
        public async Task Handle(UpdatePersonalNoteCommand request, CancellationToken cancellationToken)
        {
            var userWord = await _context.UserWords.FirstOrDefaultAsync(x => x.UserId == request.UserId && x.WordId == request.WordId, cancellationToken);

            if (userWord is null)
                throw new NotFoundException(nameof(UserWord), request.WordId);

            userWord.PersonalNote = request.PersonalNote?.Trim();

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public class UpdatePersonalNoteCommandValidator : AbstractValidator<UpdatePersonalNoteCommand>
    {
        public UpdatePersonalNoteCommandValidator()
        {
            RuleFor(x => x.UserId)
    .NotEmpty().WithMessage("Kullanıcı kimliği (UserId) zorunludur.");

            RuleFor(x => x.WordId)
                .NotEmpty().WithMessage("Kelime kimliği (WordId) zorunludur.");

            RuleFor(x => x.PersonalNote)
                .MaximumLength(1000).WithMessage("Kişisel not 1000 karakteri geçmemelidir.")
                .When(x => x.PersonalNote is not null);
        }
    }
}
