using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WordWise.Application.Common.Models;
using WordWise.Application.Features.UserWords.Commands.SaveWord;
using WordWise.Application.Features.UserWords.Commands.UnsaveWord;
using WordWise.Application.Features.UserWords.Commands.UpdatePersonalNote;
using WordWise.Application.Features.UserWords.Dtos;
using WordWise.Application.Features.UserWords.Queries.GetReviewWords;
using WordWise.Application.Features.UserWords.Queries.GetSavedWords;

namespace WordWise.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserWordController(IMediator _mediator) : ControllerBase
    {
        private Guid CurrentUserId =>
            Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<SavedWordDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSavedWords(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(
                new GetSavedWordsQuery(CurrentUserId, page, pageSize),
                cancellationToken);

            return Ok(ApiResponse<PagedResponse<SavedWordDto>>.Ok(result));
        }

        [HttpGet("review")]
        [ProducesResponseType(typeof(ApiResponse<List<ReviewWordDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReviewWords(
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(
                new GetReviewWordsQuery(CurrentUserId),
                cancellationToken);

            return Ok(ApiResponse<List<ReviewWordDto>>.Ok(
                result, $"{result.Count} kelime incelemeye hazır."));
        }

        // POST /api/userword/{wordId}
        // Kelimeyi favorilere ekler
        [HttpPost("{wordId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SaveWord(
            Guid wordId,
            CancellationToken cancellationToken)
        {
            var id = await _mediator.Send(
                new SaveWordCommand(CurrentUserId, wordId),
                cancellationToken);

            return StatusCode(201, ApiResponse<Guid>.Created(id, "Kelime başarıyla kaydedildi."));
        }

        // DELETE /api/userword/{wordId}
        // Kelimeyi favorilerden çıkarır
        [HttpDelete("{wordId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnsaveWord(
            Guid wordId,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(
                new UnsaveWordCommand(CurrentUserId, wordId),
                cancellationToken);

            return Ok(ApiResponse.OkNoData("Kelime listenizden silindi."));
        }

        // PATCH /api/userword/{wordId}/note
        // Kişisel not ekler veya günceller
        [HttpPatch("{wordId:guid}/note")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNote(
            Guid wordId,
            [FromBody] UpdatePersonalNoteDto dto,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(
                new UpdatePersonalNoteCommand(CurrentUserId, wordId, dto.PersonalNote),
                cancellationToken);

            return Ok(ApiResponse.OkNoData("Kişisel not güncellendi."));
        }
    }
}
