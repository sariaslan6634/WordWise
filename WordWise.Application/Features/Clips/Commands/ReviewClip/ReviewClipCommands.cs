using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Clips.Dtos;
using WordWise.Domain.Entities;
using WordWise.Domain.Enums;

namespace WordWise.Application.Features.Clips.Commands.ReviewClip
{
    public record PublishClipCommand(Guid ClipId) : IRequest<VideoClipDto>;

    public class PublishClipCommandHandler(IWordWiseDbContext _context)
        : IRequestHandler<PublishClipCommand, VideoClipDto>
    {
        public async Task<VideoClipDto> Handle(PublishClipCommand request, CancellationToken cancellationToken)
        {
            var clip = await _context.VideoClips.FirstOrDefaultAsync(x => x.Id == request.ClipId, cancellationToken)
                ?? throw new NotFoundException(nameof(VideoClip), request.ClipId);

            if (clip.QcStatus != ClipQcStatus.Passed)
                throw new BusinessException("Kalite kontrolünden geçmeyen klip yayınlanamaz.");

            clip.IsPublished = true;
            clip.IsRejected = false;
            await _context.SaveChangesAsync(cancellationToken);
            return VideoClipMapper.ToDto(clip);
        }
    }

    public record RejectClipCommand(Guid ClipId) : IRequest<VideoClipDto>;

    public class RejectClipCommandHandler(IWordWiseDbContext _context)
        : IRequestHandler<RejectClipCommand, VideoClipDto>
    {
        public async Task<VideoClipDto> Handle(RejectClipCommand request, CancellationToken cancellationToken)
        {
            var clip = await _context.VideoClips.FirstOrDefaultAsync(x => x.Id == request.ClipId, cancellationToken)
                ?? throw new NotFoundException(nameof(VideoClip), request.ClipId);

            clip.IsRejected = true;
            clip.IsPublished = false;
            await _context.SaveChangesAsync(cancellationToken);
            return VideoClipMapper.ToDto(clip);
        }
    }
}
