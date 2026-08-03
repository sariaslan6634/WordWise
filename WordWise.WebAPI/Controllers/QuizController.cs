using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WordWise.Application.Common.Models;
using WordWise.Application.Features.Quizzes.Commands.SubmitQuizAnswer;
using WordWise.Application.Features.Quizzes.Dtos;
using WordWise.Application.Features.Quizzes.Queries.GetQuizQuestionByVideoId;

namespace WordWise.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuizController(IMediator _mediator) : ControllerBase
    {
        [HttpGet("video/{videoId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<QuizQuestionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByVideoId(
            Guid videoId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetQuizQuestionByVideoIdQuery(videoId), cancellationToken);

            return Ok(ApiResponse<QuizQuestionDto>.Ok(result, "Test sorusu bulundu."));
        }

        [HttpPost("answer")]
        [ProducesResponseType(typeof(ApiResponse<QuizAnswerResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SubmitAnswer(
            [FromBody] SubmitAnswerRequest request,
            CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _mediator.Send(
                new SubmitQuizAnswerCommand(
                    userId,
                    request.QuizQuestionId,
                    request.GivenAnswer),
                cancellationToken);

            return Ok(ApiResponse<QuizAnswerResultDto>.Ok(result, result.Message));
        }
    }
    public record SubmitAnswerRequest(
    Guid QuizQuestionId,
    string GivenAnswer);
}
