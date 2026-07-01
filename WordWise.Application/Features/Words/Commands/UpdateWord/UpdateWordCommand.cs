using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WordWise.Application.Common.Constants;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Domain.Entities;
using WordWise.Domain.Enums;

namespace WordWise.Application.Features.Words.Commands.UpdateWord
{
    public record UpdateWordRequest(
    string Text,
    string Definition,
    string? Ipa,
    string? PartOfSpeech,
    CefrLevel? CefrLevel,
    List<string> ExampleSentences,
    List<string> Synonyms,
    List<string> Antonyms,
    string? Category,
    bool IsPublished);

    public record UpdateWordCommand(
        Guid Id,
        string Text,
        string Definition,
        string? Ipa,
        string? PartOfSpeech,
        CefrLevel? CefrLevel,
        List<string> ExampleSentences,
        List<string> Synonyms,
        List<string> Antonyms,
        string? Category,
        bool IsPublished) : IRequest;
    public class UpdateWordCommandHandler(
        IWordWiseDbContext _context,
        ICacheService _cache)
        : IRequestHandler<UpdateWordCommand>
    {
        public async Task Handle(UpdateWordCommand request, CancellationToken cancellationToken)
        {
            var word = await _context.Words.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (word is null)
                throw new NotFoundException(nameof(Word), request.Id);

            var textConflict = await _context.Words.AnyAsync(x => 
            x.Text.ToLower() == request.Text.ToLower().Trim() &&  x.Id != request.Id, cancellationToken);

            if(textConflict)
                throw new ConflictException("Word", $"'{request.Text}' zaten mevcut.");

            var oldText = word.Text;
            word.Text = request.Text.ToLowerInvariant().Trim();
            word.Definition = request.Definition.Trim();
            word.Ipa = request.Ipa?.Trim();
            word.PartOfSpeech = request.PartOfSpeech?.Trim();
            word.CefrLevel = request.CefrLevel;
            word.Category = request.Category?.Trim();
            word.IsPublished = request.IsPublished;

            word.ExampleSentencesJson = request.ExampleSentences.Any()
                ? JsonSerializer.Serialize(request.ExampleSentences)
                : null;
            word.SynonymsJson = request.Synonyms.Any()
                ? JsonSerializer.Serialize(request.Synonyms)
                : null;
            word.AntonymsJson = request.Antonyms.Any()
                ? JsonSerializer.Serialize(request.Antonyms)
                : null;

            await _context.SaveChangesAsync(cancellationToken);

            await _cache.RemoveAsync(CacheKeys.WordByText(oldText), cancellationToken);
            await _cache.RemoveAsync(CacheKeys.WordByText(word.Text), cancellationToken);

        }
    }

    public class UpdateWordCommandValidator : AbstractValidator<UpdateWordCommand>
    {
        public UpdateWordCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Kelime Id'si zorunludur.");

            RuleFor(x => x.Text)
                .NotEmpty().WithMessage("Kelime metni zorunludur.")
                .MaximumLength(100).WithMessage("Kelime metni en fazla 100 karakter olabilir.");

            RuleFor(x => x.Definition)
                .NotEmpty().WithMessage("Tanım zorunludur.")
                .MaximumLength(2000).WithMessage("Tanım en fazla 2000 karakter olabilir.");

            RuleFor(x => x.Ipa)
                .MaximumLength(100).WithMessage("IPA en fazla 100 karakter olabilir.")
                .When(x => x.Ipa is not null);

            RuleFor(x => x.PartOfSpeech)
                .MaximumLength(50).WithMessage("Kelime türü en fazla 50 karakter olabilir.")
                .When(x => x.PartOfSpeech is not null);

            RuleFor(x => x.Category)
                .MaximumLength(100).WithMessage("Kategori en fazla 100 karakter olabilir.")
                .When(x => x.Category is not null);
        }
    }
}
