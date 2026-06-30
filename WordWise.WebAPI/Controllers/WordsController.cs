using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WordWise.Application.Common.Models;
using WordWise.Application.Features.Words.Dtos;
using WordWise.Application.Features.Words.Queries.GetWordByText;
using WordWise.Domain.Enums;

namespace WordWise.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WordsController(IMediator _mediator) : ControllerBase
    {
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<WordDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
           [FromQuery] string text,
           CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetWordByTextQuery(text), cancellationToken);
            return Ok(ApiResponse<WordDetailDto>.Ok(result, "Kelime Bulundu."));
        }
    }
}
