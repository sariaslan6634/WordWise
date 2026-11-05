using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WordWise.Application.Common.Models;
using WordWise.Application.Features.Clips.Commands.GenerateClips;
using WordWise.Application.Features.Clips.Commands.ReviewClip;
using WordWise.Application.Features.Clips.Commands.RunDiscovery;
using WordWise.Application.Features.Clips.Models;
using WordWise.Application.Features.Clips.Dtos;
using WordWise.Application.Features.Clips.Queries.GetClips;
using WordWise.Application.Features.Videos.Commands.ApproveVideoCandidate;
using WordWise.Application.Features.Videos.Commands.FetchVideoCandidates;
using WordWise.Application.Features.Videos.Commands.RejectVideoCandidate;
using WordWise.Application.Features.Videos.Dtos;
using WordWise.Application.Features.Videos.Queries.GetVideoCandidateReview;
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

        [HttpGet("candidates/{candidateId:guid}/review")]
        [ProducesResponseType(typeof(ApiResponse<VideoCandidateReviewDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCandidateReview(
            Guid candidateId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetVideoCandidateReviewQuery(candidateId), cancellationToken);

            return Ok(ApiResponse<VideoCandidateReviewDto>.Ok(result));
        }

        [HttpPost("fetch-candidates/{wordId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> FetchCandidates(
            Guid wordId,
            CancellationToken cancellationToken)
        {
            var count = await _mediator.Send(
                new FetchVideoCandidatesCommand(wordId), cancellationToken);

            return Ok(ApiResponse<int>.Ok(count, $"{count} video candidates fetched."));
        }

        [HttpPost("candidates/{candidateId:guid}/generate-clips")]
        [ProducesResponseType(typeof(ApiResponse<List<VideoClipDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateClips(
            Guid candidateId,
            CancellationToken cancellationToken)
        {
            var clips = await _mediator.Send(new GenerateClipsCommand(candidateId), cancellationToken);
            var passed = clips.Count(c => c.QcStatus == "Passed");
            return Ok(ApiResponse<List<VideoClipDto>>.Ok(clips, $"{clips.Count} clips generated, {passed} passed quality control."));
        }

        /// <summary>Kelimeden bağımsız: bir YouTube bağlantısından/kimliğinden klip üretir.</summary>
        [HttpPost("clips/from-video")]
        [ProducesResponseType(typeof(ApiResponse<List<VideoClipDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateClipsFromVideo(
            [FromBody] GenerateClipsFromVideoDto dto,
            CancellationToken cancellationToken)
        {
            var clips = await _mediator.Send(new GenerateClipsFromVideoCommand(dto.Url), cancellationToken);
            var passed = clips.Count(c => c.QcStatus == "Passed");
            return Ok(ApiResponse<List<VideoClipDto>>.Ok(clips, $"{clips.Count} clips generated, {passed} passed quality control."));
        }

        /// <summary>Otomatik keşif ajanını hemen bir kez çalıştırır (zamanlayıcıyı beklemeden).</summary>
        [HttpPost("auto-discovery/run")]
        [ProducesResponseType(typeof(ApiResponse<DiscoveryRunResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RunAutoDiscovery(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new RunDiscoveryCommand(), cancellationToken);
            return Ok(ApiResponse<DiscoveryRunResult>.Ok(result,
                $"{result.VideosProcessed} videos processed, {result.ClipsCreated} clips created, {result.ClipsPublished} published."));
        }

        [HttpGet("clips")]
        [ProducesResponseType(typeof(ApiResponse<List<VideoClipDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecentClips(CancellationToken cancellationToken)
        {
            var clips = await _mediator.Send(new GetRecentClipsQuery(), cancellationToken);
            return Ok(ApiResponse<List<VideoClipDto>>.Ok(clips));
        }

        [HttpPost("clips/{clipId:guid}/publish")]
        [ProducesResponseType(typeof(ApiResponse<VideoClipDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PublishClip(Guid clipId, CancellationToken cancellationToken)
        {
            var clip = await _mediator.Send(new PublishClipCommand(clipId), cancellationToken);
            return Ok(ApiResponse<VideoClipDto>.Ok(clip, "Clip published."));
        }

        [HttpPost("clips/{clipId:guid}/reject")]
        [ProducesResponseType(typeof(ApiResponse<VideoClipDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectClip(Guid clipId, CancellationToken cancellationToken)
        {
            var clip = await _mediator.Send(new RejectClipCommand(clipId), cancellationToken);
            return Ok(ApiResponse<VideoClipDto>.Ok(clip, "Clip rejected."));
        }

        [HttpGet("clips/by-video/{youtubeId}")]
        [ProducesResponseType(typeof(ApiResponse<List<VideoClipDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClipsByVideo(string youtubeId, CancellationToken cancellationToken)
        {
            var clips = await _mediator.Send(new GetClipsByYoutubeIdQuery(youtubeId), cancellationToken);
            return Ok(ApiResponse<List<VideoClipDto>>.Ok(clips));
        }

        [HttpGet("candidates/{candidateId:guid}/clips")]
        [ProducesResponseType(typeof(ApiResponse<List<VideoClipDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClips(
            Guid candidateId,
            CancellationToken cancellationToken)
        {
            var clips = await _mediator.Send(new GetClipsByCandidateQuery(candidateId), cancellationToken);
            return Ok(ApiResponse<List<VideoClipDto>>.Ok(clips));
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
