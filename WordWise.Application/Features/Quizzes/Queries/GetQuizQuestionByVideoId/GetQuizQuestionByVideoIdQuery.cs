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
using WordWise.Application.Features.Quizzes.Dtos;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Quizzes.Queries.GetQuizQuestionByVideoId
{
    public record GetQuizQuestionByVideoIdQuery(Guid VideoId) : IRequest<QuizQuestionDto>;

    public class GetQuizQuestionByVideoIdQueryHandler(IWordWiseDbContext _context) : IRequestHandler<GetQuizQuestionByVideoIdQuery, QuizQuestionDto>
    {
        public async Task<QuizQuestionDto> Handle(GetQuizQuestionByVideoIdQuery request, CancellationToken cancellationToken)
        {
            var question = await _context.QuizQuestions
                .Where(x => x.VideoId == request.VideoId && x.IsPublished)
                .FirstOrDefaultAsync(cancellationToken);

            if (question is null)
                throw new NotFoundException(nameof(QuizQuestion), request.VideoId);

            return new QuizQuestionDto
            {
                Id = question.Id,
                VideoId = question.VideoId,
                QuestionText = question.QuestionText,
                IsFreeText = question.IsFreeText,
                Options = string.IsNullOrWhiteSpace(question.OptionsJson)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(question.OptionsJson)
                      ?? new List<string>()
            };
        }
    }

}
