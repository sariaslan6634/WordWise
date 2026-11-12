using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.WordAudio;

namespace WordWise.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WordAudioController(IWordAudioService _audio) : ControllerBase
    {
        /// <summary>Kelimenin telaffuz ses dosyası. accent: "us" (Amerikan) veya "gb" (İngiliz).</summary>
        [HttpGet("{accent}/{word}")]
        public async Task<IActionResult> Get(string accent, string word, CancellationToken cancellationToken)
        {
            accent = accent.ToLowerInvariant();
            if (accent != WordAudioIndex.Us && accent != WordAudioIndex.Gb) return NotFound();

            var entry = await _audio.FindOrFetchAsync(word, accent, cancellationToken);
            if (entry is null || !System.IO.File.Exists(entry.Path)) return NotFound();

            Response.Headers.CacheControl = "public, max-age=86400";
            return PhysicalFile(entry.Path, entry.ContentType, enableRangeProcessing: true);
        }
    }
}
