using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WordWise.Application.Common.Models;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Words.Commands.BulkImportWords;
using WordWise.Application.Features.Words.Commands.CreateWord;
using WordWise.Application.Features.Words.Commands.DeleteWord;
using WordWise.Application.Features.Words.Commands.PublishWord;
using WordWise.Application.Features.Words.Commands.UpdateWord;
using WordWise.Application.Features.Videos.Commands.FetchVideoCandidates;
using WordWise.Application.Features.Words.Dtos;
using WordWise.Application.Features.Words.Queries.GetAllWords;
using WordWise.Domain.Enums;

namespace WordWise.WebAPI.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/[controller]")]
    public class AdminWordController(IMediator _mediator, IDatamuseService _datamuseService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<WordSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchText = null,
            [FromQuery] bool? isApproved = null,
            [FromQuery] CefrLevel? cefrLevel = null,
            [FromQuery] string? partOfSpeech = null,
            [FromQuery] string? status = null,
            CancellationToken cancellationToken =default)
        {
            var result = await _mediator.Send(
                new GetAllWordsQuery(page, pageSize, searchText, isApproved, cefrLevel, partOfSpeech, status), cancellationToken);

            return Ok(ApiResponse<PagedResponse<WordSummaryDto>>.Ok(result));
        }
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteWordCommand(id), cancellationToken);
            return Ok(ApiResponse.OkNoData("Kelime silindi."));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody] CreateWordCommand command,
            CancellationToken cancellationToken)
        {
            var wordId = await _mediator.Send(command, cancellationToken);
            return StatusCode(201, ApiResponse<Guid>.Created(wordId, "Kelime eklendi."));
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] CreateWordDto dto,
            CancellationToken cancellationToken)
        {
            var command = new UpdateWordCommand(
                id,
                dto.Text,
                dto.Definition,
                dto.Ipa,
                dto.PartOfSpeech,
                dto.CefrLevel,
                dto.ExampleSentences,
                dto.Synonyms,
                dto.Antonyms,
                dto.Category,
                dto.IsPublished);

            await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse.OkNoData("Kelime güncellendi"));
        }
        [HttpPost("bulk-import")]
        [ProducesResponseType(typeof(ApiResponse<WordImportResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkImport(
            [FromBody] BulkImportWordsCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(ApiResponse<WordImportResultDto>.Ok(
                result,
                $"{result.SuccessCount} words imported, " +
                $"{result.SkippedCount} skipped, " +
                $"{result.FailedCount} failed."));
        }

        [HttpPost("daily-candidates")]
        public async Task<IActionResult> ImportDailyCandidates(CancellationToken cancellationToken)
        {
            var words = await _datamuseService.GetDailyWordSuggestionsAsync(cancellationToken);
            if (words.Count == 0)
                return StatusCode(503, ApiResponse<WordImportResultDto>.Fail(
                    "Günlük kelime kaynağı şu anda yanıt vermiyor.", StatusCodes.Status503ServiceUnavailable));

            var result = await _mediator.Send(new BulkImportWordsCommand(words), cancellationToken);
            return Ok(ApiResponse<WordImportResultDto>.Ok(result,
                $"{result.SuccessCount} kelime adayı eklendi; {result.SkippedCount} mevcut, {result.FailedCount} sözlükte bulunamadı."));
        }

        [HttpPut("{id:guid}/publish")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Publish(
    Guid id,
    CancellationToken cancellationToken)
        {
            await _mediator.Send(new PublishWordCommand(id), cancellationToken);
            return Ok(ApiResponse.OkNoData("Word published successfully."));
        }
    }
}
