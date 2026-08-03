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
    public record GetReviewWordsQuery(Guid UserId) : IRequest<List<ReviewWordDto>>;

    public class GetReviewWordsQueryHandler(IWordWiseDbContext _context) : IRequestHandler<GetReviewWordsQuery, List<ReviewWordDto>>
    {
        public async Task<List<ReviewWordDto>> Handle(GetReviewWordsQuery request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            var userWords = await _context.UserWords
                .Include(x => x.Word)
                .Where(x => x.UserId == request.UserId && x.NextReviewAt <= now)
                .OrderBy(x => x.KnownLevel)
                .ThenBy(x => x.NextReviewAt)
                .ToListAsync(cancellationToken);

            return userWords.Select(uw => new ReviewWordDto
            {
                UserWordId = uw.Id,
                WordId = uw.WordId,
                Text = uw.Word.Text,
                Definition = uw.Word.Definition,
                Ipa = uw.Word.Ipa,
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
