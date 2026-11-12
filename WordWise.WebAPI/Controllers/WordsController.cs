using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WordWise.Application.Common.Models;
using WordWise.Application.Features.Words.Dtos;
using WordWise.Application.Features.Words.Queries.GetWordByText;
using WordWise.Application.Features.Words.Queries.SuggestWords;
using WordWise.Domain.Enums;

namespace WordWise.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WordsController(IMediator _mediator) : ControllerBase
    {
        /// <summary>Arama kutusu önerileri: yazılan harflerle başlayan yayındaki kelimeler.</summary>
        [HttpGet("suggest")]
        [ProducesResponseType(typeof(ApiResponse<List<WordSuggestionDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Suggest(
            [FromQuery] string prefix = "",
            [FromQuery] int limit = 8,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(new SuggestWordsQuery(prefix ?? string.Empty, limit), cancellationToken);
            return Ok(ApiResponse<List<WordSuggestionDto>>.Ok(result));
        }

        /// <summary>
        /// Altyazıda tıklanan sözcüğün anlamı: önce sözcüğün kendisi, yoksa olası ana biçimleri (giving → give) aranır.
        /// </summary>
        [HttpGet("lookup")]
        [ProducesResponseType(typeof(ApiResponse<WordDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Lookup([FromQuery] string text, CancellationToken cancellationToken)
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Guid? userId = Guid.TryParse(claim, out var parsed) ? parsed : null;
            foreach (var candidate in WordWise.Application.Features.Words.WordForms.Candidates(text).Take(12))
            {
                try
                {
                    var result = await _mediator.Send(new GetWordByTextQuery(candidate, userId), cancellationToken);
                    return Ok(ApiResponse<WordDetailDto>.Ok(result, "Kelime Bulundu."));
                }
                catch (WordWise.Application.Common.Exceptions.NotFoundException) { /* sıradaki adayı dene */ }
            }
            throw new WordWise.Application.Common.Exceptions.NotFoundException("Word", text);
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<WordDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search(
           [FromQuery] string text,
           CancellationToken cancellationToken)
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Guid? userId = Guid.TryParse(claim, out var parsed) ? parsed : null;
            var result = await _mediator.Send(new GetWordByTextQuery(text, userId), cancellationToken);
            return Ok(ApiResponse<WordDetailDto>.Ok(result, "Kelime Bulundu."));
        }
    }
}
