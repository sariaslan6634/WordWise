using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Interfaces;

namespace WordWise.Application.Features.Words.Queries.SuggestWords
{
    public record WordSuggestionDto(string Text, string? PartOfSpeech);

    /// <summary>Arama kutusu önerileri: yayındaki kelimelerden, yazılan harflerle başlayanlar (kısa kelimeler önce).</summary>
    public record SuggestWordsQuery(string Prefix, int Limit = 8) : IRequest<List<WordSuggestionDto>>;

    public class SuggestWordsQueryHandler(IWordWiseDbContext _context)
        : IRequestHandler<SuggestWordsQuery, List<WordSuggestionDto>>
    {
        public async Task<List<WordSuggestionDto>> Handle(SuggestWordsQuery request, CancellationToken cancellationToken)
        {
            // Sadece harf, kesme işareti ve tire: LIKE joker karakterleri (%, _) sorguya hiç girmez.
            var prefix = new string(request.Prefix.ToLowerInvariant().Where(c => char.IsLetter(c) || c == '\'' || c == '-').ToArray());
            if (prefix.Length == 0) return new();

            var limit = Math.Clamp(request.Limit, 1, 20);
            return await _context.Words
                .Where(x => x.IsPublished && x.Text.StartsWith(prefix))
                .OrderBy(x => x.Text.Length)
                .ThenBy(x => x.Text)
                .Take(limit)
                .Select(x => new WordSuggestionDto(x.Text, x.PartOfSpeech))
                .ToListAsync(cancellationToken);
        }
    }
}
