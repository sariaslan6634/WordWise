using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Application.Common.Constants;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Words.Commands.PublishWord
{
    public record PublishWordCommand(Guid WordId) : IRequest;


    public class PublishWordCommandHandler(
        IWordWiseDbContext _context,
        ICacheService _cache)
        : IRequestHandler<PublishWordCommand>
    {
        public async Task Handle(PublishWordCommand request, CancellationToken cancellationToken)
        {
            var word = await _context.Words.FirstOrDefaultAsync(x => x.Id == request.WordId, cancellationToken);

            if (word is null)
                throw new NotFoundException(nameof(Word), request.WordId);
            if(word.IsPublished)
                throw new BusinessException("Word is already published.");

            var hasApprovedVideo = await _context.Videos.AnyAsync(x=>x.WordId == request.WordId && x.IsPublished, cancellationToken);

            if (!hasApprovedVideo)
                throw new BusinessException("Word must have at least one approved video before publishing.");

            word.IsPublished = true;

            await _context.SaveChangesAsync(cancellationToken);

            await _cache.RemoveAsync(CacheKeys.WordByText(word.Text), cancellationToken); 
        }
    }
    public class PublishWordCommandValidator : AbstractValidator<PublishWordCommand>
    {
        public PublishWordCommandValidator()
        {
            RuleFor(x => x.WordId)
                .NotEmpty().WithMessage("WordId is required.");
        }
    }
}
