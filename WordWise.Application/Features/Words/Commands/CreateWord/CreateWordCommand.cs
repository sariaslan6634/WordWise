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
using WordWise.Application.Features.Videos.Commands.FetchVideoCandidates;
using WordWise.Domain.Entities;
using WordWise.Domain.Enums;

namespace WordWise.Application.Features.Words.Commands.CreateWord
{
    public record CreateWordCommand(
        string Text,
        string Definition,
        string? Ipa,
        string? PartOfSpeech,
        CefrLevel? CefrLevel,
        List<string> ExampleSentences,
        List<string> Synonyms,
        List<string> Antonyms,
        string? Category,
        bool IsPublished) : IRequest<Guid>;
    public class CreateWordCommandhandler(IWordWiseDbContext _context,
        ICacheService _cache,IMediator _mediator) : IRequestHandler<CreateWordCommand, Guid>
    {
        public async Task<Guid> Handle(CreateWordCommand request, CancellationToken cancellationToken)
        {
            var exists = await _context.Words.AnyAsync(x => x.Text.ToLower() == request.Text.ToLower().Trim(), cancellationToken);
            if (exists)
                throw new ConflictException("Word", $"'{request.Text}' kelimesi zaten kayıtlı.");

            var word = new Word
            {
                Text = request.Text.ToLowerInvariant().Trim(),
                Definition = request.Definition.Trim(),
                Ipa = request.Ipa?.Trim(),
                PartOfSpeech = request.PartOfSpeech?.Trim(),
                CefrLevel = request.CefrLevel,
                Category = request.Category?.Trim(),
                IsPublished = request.IsPublished,
                SourceProvider = "Manual",

                ExampleSentencesJson = request.ExampleSentences.Any()
                    ? JsonSerializer.Serialize(request.ExampleSentences)
                    : null,
                SynonymsJson = request.Synonyms.Any()
                    ? JsonSerializer.Serialize(request.Synonyms)
                    : null,
                AntonymsJson = request.Antonyms.Any()
                    ? JsonSerializer.Serialize(request.Antonyms)
                    : null,
            };

            await _context.Words.AddAsync(word, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _ = Task.Run(() => _mediator.Send(
                new FetchVideoCandidatesCommand(word.Id, word.Text),
                CancellationToken.None), CancellationToken.None);

            return word.Id;
        }
    }

    public class CreateWordCommandValidator : AbstractValidator<CreateWordCommand>
    {
        public CreateWordCommandValidator()
        {
            RuleFor(x => x.Text)
                .NotEmpty().WithMessage("Kelime metni zorunludur.")
                .MaximumLength(100).WithMessage("Kelime metni 100 karakteri geçmemelidir.");
            RuleFor(x => x.Definition)
                .NotEmpty().WithMessage("Açıklama zorunludur.")
                .MaximumLength(1000).WithMessage("Açıklama 2000 karakteri geçmemelidir.");
            RuleFor(x => x.Ipa)
                .MaximumLength(100).WithMessage("IPA 100 karakteri geçmemelidir.");
            RuleFor(x => x.PartOfSpeech)
                .MaximumLength(50).WithMessage("Kelime türü 50 karakteri geçmemelidir.");
            RuleFor(x => x.Category)
                .MaximumLength(100).WithMessage("Kategori 100 karakteri geçmemelidir.")
                .When(x => x.Category is not null);

            RuleForEach(x => x.ExampleSentences)
                .MaximumLength(500).WithMessage("Her bir örnek cümle 500 karakteri geçmemelidir.");

            RuleForEach(x => x.Synonyms)
                .MaximumLength(100).WithMessage("Her bir eş anlamlı kelime 100 karakteri geçmemelidir.");

            RuleForEach(x => x.Antonyms)
                .MaximumLength(100).WithMessage("Her bir zıt anlamlı kelime 100 karakteri geçmemelidir.");
        }
    }
}
