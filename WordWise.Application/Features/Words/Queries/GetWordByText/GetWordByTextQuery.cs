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
    /// <param name="UserId">Verilirse sonuçta kelimenin bu üye tarafından kaydedilip kaydedilmediği (IsSaved) de döner.</param>
    public record GetWordByTextQuery(string text, Guid? UserId = null) : IRequest<WordDetailDto>;

    public class GetWordByTextQueryHandler(IWordWiseDbContext _context,
        ICacheService _cache,
        IMapper _mapper,
        IWordAudioService _audio) : IRequestHandler<GetWordByTextQuery, WordDetailDto>
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

            // Ses ve kaydedilme durumu önbelleğe girmez (kullanıcıya özel / klasör sonradan güncellenebilir);
            // önbellekteki ortak nesneyi bozmamak için kopya üzerinde çalışılır.
            var response = result.Clone();
            response.AudioAccents = _audio.GetAccents(result.Text).ToList();
            if (request.UserId is Guid userId)
                response.IsSaved = await _context.UserWords
                    .AnyAsync(x => x.UserId == userId && x.WordId == result.Id, cancellationToken);
            return response;
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
