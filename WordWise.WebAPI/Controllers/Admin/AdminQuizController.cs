using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WordWise.Application.Common.Models;
using WordWise.Application.Features.Quizzes.Commands.CreateQuizQuestion;
using WordWise.Application.Features.Quizzes.Dtos;

namespace WordWise.WebAPI.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminQuizController(IMediator _mediator) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create(
            [FromBody] CreateQuizQuestionDto dto,
            CancellationToken cancellationToken)
        {
            var command = new CreateQuizQuestionCommand(
                dto.VideoId,
                dto.QuestionText,
                dto.Options,
                dto.CorrectAnswer,
                dto.IsFreeText,
                dto.IsPublished
                );

            var questionId = await _mediator.Send(command, cancellationToken);
            return StatusCode(201, ApiResponse<Guid>.Created(questionId, "Test sorusu başarıyla oluşturuldu."));
        }
    }
}
