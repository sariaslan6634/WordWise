using MediatR;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Features.Clips.Dtos;
using WordWise.Application.Features.Clips.Services;

namespace WordWise.Application.Features.Clips.Commands.GenerateClips
{
    /// <summary>Kelimeden bağımsız: herhangi bir YouTube bağlantısı/kimliğinden klip üretir.</summary>
    public record GenerateClipsFromVideoCommand(string UrlOrId) : IRequest<List<VideoClipDto>>;

    public class GenerateClipsFromVideoCommandHandler(ClipGenerator _generator)
        : IRequestHandler<GenerateClipsFromVideoCommand, List<VideoClipDto>>
    {
        public async Task<List<VideoClipDto>> Handle(GenerateClipsFromVideoCommand request, CancellationToken cancellationToken)
        {
            var youtubeId = ClipGenerator.ExtractYoutubeId(request.UrlOrId)
                ?? throw new BusinessException("Geçerli bir YouTube bağlantısı veya video kimliği girin.");

            return await _generator.GenerateAsync(youtubeId, null, cancellationToken);
        }
    }
}
