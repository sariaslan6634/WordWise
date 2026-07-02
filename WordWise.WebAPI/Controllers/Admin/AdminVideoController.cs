using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WordWise.Application.Common.Models;
using WordWise.Application.Features.Videos.Commands.ApproveVideoCandidate;
using WordWise.Application.Features.Videos.Commands.FetchVideoCandidates;
using WordWise.Application.Features.Videos.Commands.RejectVideoCandidate;
using WordWise.Application.Features.Videos.Dtos;
using WordWise.Application.Features.Videos.Queries.GetVideoCandidatesByWordId;

namespace WordWise.WebAPI.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminVideoController(IMediator _mediator) : ControllerBase
    {
        [HttpGet("candidates/{wordId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<List<VideoCandidateDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCandidates(
            Guid wordId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetVideoCandidatesByWordIdQuery(wordId), cancellationToken);

            return Ok(ApiResponse<List<VideoCandidateDto>>.Ok(result));
        }

        [HttpPost("fetch-candidates/{wordId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> FetchCandidates(
            Guid wordId,
            [FromQuery] string wordText,
            CancellationToken cancellationToken)
        {
            var count = await _mediator.Send(
                new FetchVideoCandidatesCommand(wordId, wordText), cancellationToken);

            return Ok(ApiResponse<int>.Ok(count, $"{count} video candidates fetched."));
        }

        [HttpPost("candidates/{candidateId:guid}/approve")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Approve(
            Guid candidateId,
            [FromBody] ApproveVideoCandidateDto dto,
            CancellationToken cancellationToken)
        {
            var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var videoId = await _mediator.Send(
                new ApproveVideoCandidateCommand(
                    candidateId,
                    adminId,
                    dto.StartSec,
                    dto.EndSec,
                    dto.Transcript),
                cancellationToken);

            return StatusCode(201, ApiResponse<Guid>.Created(videoId, "Video approved and published."));
        }

        [HttpPost("candidates/{candidateId:guid}/reject")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Reject(
            Guid candidateId,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(
                new RejectVideoCandidateCommand(candidateId), cancellationToken);

            return Ok(ApiResponse.OkNoData("Video candidate rejected."));
        }
    }
}
