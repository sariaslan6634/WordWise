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

    /// <summary>Videonun tüm kliplerinde "videoda yazılı altyazı var" işaretini değiştirir.</summary>
    public record SetBurnedInCaptionsCommand(string YoutubeId, bool Value) : IRequest<int>;

    public class SetBurnedInCaptionsCommandHandler(IWordWiseDbContext _context)
        : IRequestHandler<SetBurnedInCaptionsCommand, int>
    {
        public async Task<int> Handle(SetBurnedInCaptionsCommand request, CancellationToken cancellationToken)
        {
            var clips = await _context.VideoClips
                .Where(x => x.YoutubeId == request.YoutubeId)
                .ToListAsync(cancellationToken);
            if (clips.Count == 0)
                throw new NotFoundException(nameof(VideoClip), request.YoutubeId);

            foreach (var clip in clips) clip.HasBurnedInCaptions = request.Value;
            await _context.SaveChangesAsync(cancellationToken);
            return clips.Count;
        }
    }

    /// <summary>
    /// Onay bekleyen (yayınlanmamış) klibin başlangıç/bitiş saniyesini elle ayarlar; örn. cümlenin sonu kesiliyorsa bitişi uzatmak için.
    /// Yayındaki klip değiştirilemez (öğrenciler görüyor/kaydetmiş olabilir): önce yayından kaldırılmalı.
    /// </summary>
    public record AdjustClipTimingCommand(Guid ClipId, double StartSec, double EndSec) : IRequest<VideoClipDto>;

    public class AdjustClipTimingCommandHandler(IWordWiseDbContext _context)
        : IRequestHandler<AdjustClipTimingCommand, VideoClipDto>
    {
        public const double MaxClipSeconds = 60;

        public async Task<VideoClipDto> Handle(AdjustClipTimingCommand request, CancellationToken cancellationToken)
        {
            var clip = await _context.VideoClips.FirstOrDefaultAsync(x => x.Id == request.ClipId, cancellationToken)
                ?? throw new NotFoundException(nameof(VideoClip), request.ClipId);

            if (clip.IsPublished)
                throw new BusinessException("Yayındaki klibin süresi değiştirilemez. Önce yayından kaldır, ayarla, sonra tekrar onayla.");

            var start = Math.Round(request.StartSec, 2);
            var end = Math.Round(request.EndSec, 2);
            if (double.IsNaN(start) || double.IsNaN(end) || start < 0)
                throw new BusinessException("Başlangıç saniyesi geçersiz.");
            if (end <= start)
                throw new BusinessException("Bitiş, başlangıçtan sonra olmalı.");
            if (end - start > MaxClipSeconds)
                throw new BusinessException($"Klip en fazla {MaxClipSeconds:0} saniye olabilir.");

            clip.StartSec = start;
            clip.EndSec = end;
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
