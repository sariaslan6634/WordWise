using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.UserWords.Dtos;

namespace WordWise.Application.Features.UserWords.Queries.GetReviewWords
{
    /// <param name="IncludeNotDue">true ise vakti gelmemiş kaydedilen kelimeler de tekrara dahil edilir.</param>
    /// <param name="PartOfSpeech">Verilirse sadece bu türdeki kelimeler (noun, verb...).</param>
    public record GetReviewWordsQuery(Guid UserId, bool IncludeNotDue = false, string? PartOfSpeech = null) : IRequest<List<ReviewWordDto>>;

    public class GetReviewWordsQueryHandler(IWordWiseDbContext _context, IWordAudioService _audio) : IRequestHandler<GetReviewWordsQuery, List<ReviewWordDto>>
    {
        public async Task<List<ReviewWordDto>> Handle(GetReviewWordsQuery request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            var query = _context.UserWords
                .Include(x => x.Word)
                .Where(x => x.UserId == request.UserId);
            if (!request.IncludeNotDue)
                query = query.Where(x => x.NextReviewAt <= now);
            if (!string.IsNullOrWhiteSpace(request.PartOfSpeech))
            {
                var pos = request.PartOfSpeech.Trim().ToLower();
                query = query.Where(x => x.Word.PartOfSpeech != null && x.Word.PartOfSpeech.ToLower() == pos);
            }

            var userWords = await query
                .OrderBy(x => x.KnownLevel)
                .ThenBy(x => x.NextReviewAt)
                .Take(50)
                .ToListAsync(cancellationToken);

            return userWords.Select(uw => new ReviewWordDto
            {
                UserWordId = uw.Id,
                WordId = uw.WordId,
                Text = uw.Word.Text,
                Definition = uw.Word.Definition,
                Ipa = uw.Word.Ipa,
                PartOfSpeech = uw.Word.PartOfSpeech,
                AudioAccents = _audio.GetAccents(uw.Word.Text).ToList(),
                ExampleSentences = string.IsNullOrWhiteSpace(uw.Word.ExampleSentencesJson)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(uw.Word.ExampleSentencesJson)
                      ?? new List<string>(),
                KnownLevel = uw.KnownLevel,
                NextReviewAt = uw.NextReviewAt,
            }).ToList();

        }
    }
}
