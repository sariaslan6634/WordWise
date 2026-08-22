using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Videos.Commands.FetchVideoCandidates;
using WordWise.Application.Features.Words.Dtos;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Words.Commands.BulkImportWords
{
    public record BulkImportWordsCommand(
            List<string> Words,
            bool IsPublished = false) : IRequest<WordImportResultDto>;

    // ── Handler ───────────────────────────────────────────────────────────────
    public class BulkImportWordsCommandHandler(
        IWordWiseDbContext _context,
        IFreeDictionaryService _dictionaryService,
        IDatamuseService _datamuseService,
        IServiceProvider _serviceProvider) : IRequestHandler<BulkImportWordsCommand, WordImportResultDto>
    {
        public async Task<WordImportResultDto> Handle(
            BulkImportWordsCommand request,
            CancellationToken cancellationToken)
        {
            var result = new WordImportResultDto
            {
                TotalRequested = request.Words.Count
            };

            foreach (var rawWord in request.Words)
            {
                var wordText = rawWord.ToLowerInvariant().Trim();

                if (string.IsNullOrWhiteSpace(wordText))
                    continue;

                // Zaten DB'de var mı?
                var exists = await _context.Words
                    .AnyAsync(x => x.Text == wordText, cancellationToken);

                if (exists)
                {
                    result.SkippedCount++;
                    result.SkippedWords.Add(wordText);
                    continue;
                }

                var dictResult = await _dictionaryService
                    .GetWordAsync(wordText, cancellationToken);

                if (dictResult is null)
                {
                    result.FailedCount++;
                    result.FailedWords.Add(wordText);
                    continue;
                }

                var firstMeaning = dictResult.Meanings.FirstOrDefault();
                var definition = firstMeaning?.Definitions.FirstOrDefault()?.Definition
                    ?? string.Empty;

                var exampleSentences = dictResult.Meanings
                    .SelectMany(m => m.Definitions)
                    .Where(d => !string.IsNullOrWhiteSpace(d.Example))
                    .Select(d => d.Example!)
                    .Take(3)
                    .ToList();

                var partOfSpeech = firstMeaning?.PartOfSpeech;

                var ipa = dictResult.Phonetics
                    .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Text))?.Text;

                var synonyms = await _datamuseService
                    .GetSynonymsAsync(wordText, cancellationToken);

                var antonyms = await _datamuseService
                    .GetAntonymsAsync(wordText, cancellationToken);

                var word = new Word
                {
                    Text = wordText,
                    Definition = definition,
                    Ipa = ipa,
                    PartOfSpeech = partOfSpeech,
                    IsPublished = request.IsPublished,
                    SourceProvider = "FreeDictionary",
                    SourceUrl = $"https://api.dictionaryapi.dev/api/v2/entries/en/{wordText}",
                    ImportedAt = DateTime.UtcNow,

                    ExampleSentencesJson = exampleSentences.Any()
                        ? JsonSerializer.Serialize(exampleSentences)
                        : null,

                    SynonymsJson = synonyms.Any()
                        ? JsonSerializer.Serialize(synonyms)
                        : null,

                    AntonymsJson = antonyms.Any()
                        ? JsonSerializer.Serialize(antonyms)
                        : null,
                };

                await _context.Words.AddAsync(word, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                var wordId = word.Id;

                _ = Task.Run(async () =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await mediator.Send(new FetchVideoCandidatesCommand(wordId, wordText));
                });
                result.SuccessCount++;

                await Task.Delay(300, cancellationToken);
            }

            return result;
        }
    }
    public class BulkImportWordsCommandValidator
        : AbstractValidator<BulkImportWordsCommand>
    {
        public BulkImportWordsCommandValidator()
        {
            RuleFor(x => x.Words)
                .NotEmpty().WithMessage("Word list cannot be empty.")
                .Must(w => w.Count <= 100)
                .WithMessage("Maximum 100 words can be imported at once.");

            RuleForEach(x => x.Words)
                .NotEmpty().WithMessage("Word cannot be empty.")
                .MaximumLength(100).WithMessage("Each word must not exceed 100 characters.");
        }
    }
}
