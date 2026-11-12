using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Clips.Commands.SaveClip
{
    public record SaveClipCommand(Guid UserId, Guid ClipId) : IRequest;

    /// <summary>Klibi kaydeder. Zaten kayıtlıysa sessizce başarılı sayılır; önceden kaldırılmışsa eski satır geri gelir.</summary>
    public class SaveClipCommandHandler(IWordWiseDbContext _context) : IRequestHandler<SaveClipCommand>
    {
        public async Task Handle(SaveClipCommand request, CancellationToken cancellationToken)
        {
            var clipExists = await _context.VideoClips.AnyAsync(c => c.Id == request.ClipId && !c.IsRejected, cancellationToken);
            if (!clipExists) throw new NotFoundException(nameof(VideoClip), request.ClipId);

            var existing = await _context.UserClips.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.UserId == request.UserId && u.ClipId == request.ClipId, cancellationToken);
            if (existing is null)
            {
                await _context.UserClips.AddAsync(new UserClip { UserId = request.UserId, ClipId = request.ClipId }, cancellationToken);
            }
            else if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.CreatedAt = DateTime.UtcNow; // listede yeniden en üste çıksın
            }
            else return;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public record UnsaveClipCommand(Guid UserId, Guid ClipId) : IRequest;

    /// <summary>Klibi kayıtlılardan çıkarır. Kayıtlı değilse sessizce başarılı sayılır.</summary>
    public class UnsaveClipCommandHandler(IWordWiseDbContext _context) : IRequestHandler<UnsaveClipCommand>
    {
        public async Task Handle(UnsaveClipCommand request, CancellationToken cancellationToken)
        {
            var existing = await _context.UserClips
                .FirstOrDefaultAsync(u => u.UserId == request.UserId && u.ClipId == request.ClipId, cancellationToken);
            if (existing is null) return;
            existing.IsDeleted = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
