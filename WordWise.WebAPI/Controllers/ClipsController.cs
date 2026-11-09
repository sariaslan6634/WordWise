using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WordWise.Application.Common.Models;
using WordWise.Application.Features.Clips.Commands.ReportClip;
using WordWise.Application.Features.Clips.Dtos;
using WordWise.Application.Features.Clips.Queries.GetLearnerClips;
using WordWise.Application.Features.Clips.Services;

namespace WordWise.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ClipsController(IMediator _mediator) : ControllerBase
    {
        /// <summary>Öğrenci için yayındaki kliplerden rastgele bir seçki. categories: virgülle song,twister,phrase (boş veya all = hepsi).</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<LearnerClipDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClips([FromQuery] int count = 20, [FromQuery] string? categories = null, CancellationToken cancellationToken = default)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var clips = await _mediator.Send(new GetLearnerClipsQuery(userId, count, ClipClassifier.ParseFilter(categories)), cancellationToken);
            return Ok(ApiResponse<List<LearnerClipDto>>.Ok(clips));
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
