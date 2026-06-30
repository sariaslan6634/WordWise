using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Constants;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Words.Dtos;

namespace WordWise.Application.Features.Words.Queries.GetWordByText
{
    public record GetWordByTextQuery(string text) : IRequest<WordDetailDto>;

    public class GetWordByTextQueryHandler(IWordWiseDbContext _context,
        ICacheService _cache,
        IMapper _mapper) : IRequestHandler<GetWordByTextQuery, WordDetailDto>
    {
        public async Task<WordDetailDto> Handle(GetWordByTextQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.WordByText(request.text);
            var result = await _cache.GetOrSetAsync(
                cacheKey,
                () => FetchFromDatabaseAsync(request.text,cancellationToken),
                CacheKeys.WordTtl,
                cancellationToken);
            if (result is null)
                throw new NotFoundException("Word", request.text);

            return result;
        }

        private async Task<WordDetailDto?> FetchFromDatabaseAsync(string text, CancellationToken cancellationToken)
        {
            var normalizedText = text.ToLowerInvariant().Trim();

            var word = await _context.Words
                .Include(x => x.Videos.Where(v=> !v.IsDeleted && v.IsPublished))
                .Where(x=>x.IsPublished)                
                .FirstOrDefaultAsync(x => x.Text.ToLower() == normalizedText, cancellationToken);

            if (word is null)
                return null;

            word.Videos = word.Videos.OrderBy(x => x.DisplayOrder).ToList();

            return _mapper.Map<WordDetailDto>(word);
        }

        public class GetWordByTextQueryValidator : AbstractValidator<GetWordByTextQuery>
        {
            public GetWordByTextQueryValidator()
            {
                RuleFor(x=>x.text)
                .NotEmpty().WithMessage("Aranan metin zorunludur.")
                .MaximumLength(100).WithMessage("Aranan metin 100 karakteri geçmemelidir.");
            }
        }
    }
}
