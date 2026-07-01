using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WordWise.Application.Common.Models;
using WordWise.Application.Features.Words.Commands.CreateWord;
using WordWise.Application.Features.Words.Commands.DeleteWord;
using WordWise.Application.Features.Words.Commands.UpdateWord;
using WordWise.Application.Features.Words.Dtos;
using WordWise.Application.Features.Words.Queries.GetAllWords;

namespace WordWise.WebAPI.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminWordController(IMediator _mediator) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<WordSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchText = null,
            CancellationToken cancellationToken =default)
        {
            var result = await _mediator.Send(
                new GetAllWordsQuery(page, pageSize, searchText), cancellationToken);

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
    }
}
