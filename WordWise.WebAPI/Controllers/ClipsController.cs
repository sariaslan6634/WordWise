using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WordWise.Application.Common.Models;
using WordWise.Application.Features.Clips.Commands.ReportClip;
using WordWise.Application.Features.Clips.Dtos;
using WordWise.Application.Features.Clips.Commands.SaveClip;
using WordWise.Application.Features.Clips.Queries.GetLearnerClips;
using WordWise.Application.Features.Clips.Queries.GetSavedClips;
using WordWise.Application.Features.Clips.Services;

namespace WordWise.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ClipsController(IMediator _mediator, ClipSourceBackfiller _backfiller) : ControllerBase
    {
        private Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        /// <summary>Öğrenci için rastgele klip seçkisi. savedOnly=true ise yalnızca kaydettikleri.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<LearnerClipDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClips([FromQuery] int count = 20, [FromQuery] bool savedOnly = false, CancellationToken cancellationToken = default)
        {
            var clips = await _mediator.Send(new GetLearnerClipsQuery(CurrentUserId, count, savedOnly), cancellationToken);
            await _backfiller.FillAsync(clips, cancellationToken);
            return Ok(ApiResponse<List<LearnerClipDto>>.Ok(clips));
        }

        /// <summary>Kullanıcının kaydettiği klipler (en son kaydedilen üstte).</summary>
        [HttpGet("saved")]
        [ProducesResponseType(typeof(ApiResponse<List<LearnerClipDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSavedClips(CancellationToken cancellationToken)
        {
            var clips = await _mediator.Send(new GetSavedClipsQuery(CurrentUserId), cancellationToken);
            await _backfiller.FillAsync(clips, cancellationToken);
            return Ok(ApiResponse<List<LearnerClipDto>>.Ok(clips));
        }

        [HttpPost("{clipId:guid}/save")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SaveClip(Guid clipId, CancellationToken cancellationToken)
        {
            await _mediator.Send(new SaveClipCommand(CurrentUserId, clipId), cancellationToken);
            return Ok(ApiResponse.OkNoData("Klip kaydedildi."));
        }

        [HttpDelete("{clipId:guid}/save")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UnsaveClip(Guid clipId, CancellationToken cancellationToken)
        {
            await _mediator.Send(new UnsaveClipCommand(CurrentUserId, clipId), cancellationToken);
            return Ok(ApiResponse.OkNoData("Klip kayıtlılardan çıkarıldı."));
        }

        /// <summary>Klibi bildirir (kalite, altyazı uyumsuzluğu, uygunsuz içerik...).</summary>
        [HttpPost("{clipId:guid}/report")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Report(
            Guid clipId,
            [FromBody] ReportClipRequest request,
            CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _mediator.Send(new ReportClipCommand(userId, clipId, request.Reason, request.Note), cancellationToken);
            return Ok(ApiResponse.OkNoData("Video bildirildi. Teşekkürler, ekibimiz inceleyecek."));
        }
    }
}
