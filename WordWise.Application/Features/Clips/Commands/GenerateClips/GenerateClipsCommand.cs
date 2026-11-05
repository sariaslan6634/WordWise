using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Clips.Dtos;
using WordWise.Application.Features.Clips.Services;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Clips.Commands.GenerateClips
{
    /// <summary>Kelimeye bağlı bir video adayından klip üretir.</summary>
    public record GenerateClipsCommand(Guid CandidateId) : IRequest<List<VideoClipDto>>;

    public class GenerateClipsCommandHandler(IWordWiseDbContext _context, ClipGenerator _generator)
        : IRequestHandler<GenerateClipsCommand, List<VideoClipDto>>
    {
        public async Task<List<VideoClipDto>> Handle(GenerateClipsCommand request, CancellationToken cancellationToken)
        {
            var candidate = await _context.VideoCandidates
                .FirstOrDefaultAsync(x => x.Id == request.CandidateId, cancellationToken);
            if (candidate is null)
                throw new NotFoundException(nameof(VideoCandidate), request.CandidateId);

            return await _generator.GenerateAsync(candidate.YoutubeId, candidate.Id, cancellationToken);
        }
    }

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
