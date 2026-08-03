using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Quizzes.Commands.CreateQuizQuestion
{
    public record CreateQuizQuestionCommand(
                Guid VideoId,
        string QuestionText,
        List<string> Options,
        string CorrectAnswer,
        bool IsFreeText,
        bool IsPublished) : IRequest<Guid>;

    public class CreateQuizQuestionCommandHandler(IWordWiseDbContext _context) : IRequestHandler<CreateQuizQuestionCommand, Guid>
    {
        public async Task<Guid> Handle(CreateQuizQuestionCommand request, CancellationToken cancellationToken)
        {
            var videoExists = await _context.Videos.AnyAsync(x => x.Id == request.VideoId, cancellationToken);

            if (!videoExists)
                throw new NotFoundException(nameof(Video), request.VideoId);

            if (!request.IsFreeText && !request.Options.Contains(request.CorrectAnswer))
                throw new BusinessException("Doğru cevap sunulan şıklardan olmalı.");

            var question = new QuizQuestion
            {
                VideoId = request.VideoId,
                QuestionText = request.QuestionText,
                OptionsJson = JsonSerializer.Serialize(request.Options),
                CorrectAnswer = request.CorrectAnswer.Trim(),
                IsFreeText = request.IsFreeText,
                IsPublished = request.IsPublished
            };
            await _context.QuizQuestions.AddAsync(question, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return question.Id;
        }
    }

    public class CreateQuizQuestionCommandValidator : AbstractValidator<CreateQuizQuestionCommand>
    {
        public CreateQuizQuestionCommandValidator()
        {
            RuleFor(x => x.VideoId)
                            .NotEmpty().WithMessage("Video ID zorunludur.");

            RuleFor(x => x.QuestionText)
                .NotEmpty().WithMessage("Soru metni zorunludur.")
                .MaximumLength(1000).WithMessage("Soru metni en fazla 1000 karakter olabilir.");

            RuleFor(x => x.CorrectAnswer)
                .NotEmpty().WithMessage("Doğru cevap zorunludur.")
                .MaximumLength(500).WithMessage("Doğru cevap en fazla 500 karakter olabilir.");

            // Şıklı sorularda en az 2 seçenek olmalı
            RuleFor(x => x.Options)
                .Must((cmd, options) => cmd.IsFreeText || options.Count >= 2)
                .WithMessage("Çoktan seçmeli sorular en az 2 seçeneğe sahip olmalıdır.")
                .Must((cmd, options) => cmd.IsFreeText || options.Count <= 4)
                .WithMessage("Çoktan seçmeli sorular en fazla 4 seçeneğe sahip olabilir.");
        }
    }
}
