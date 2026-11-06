using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Domain.Entities;
using WordWise.Domain.Enums;

namespace WordWise.Application.Features.Clips.Commands.ReportClip
{
    public record ReportClipCommand(Guid UserId, Guid ClipId, string Reason, string? Note) : IRequest<Unit>;

    public class ReportClipCommandHandler(IWordWiseDbContext _context)
        : IRequestHandler<ReportClipCommand, Unit>
    {
        public async Task<Unit> Handle(ReportClipCommand request, CancellationToken cancellationToken)
        {
            if (!Enum.TryParse<ClipReportReason>(request.Reason, true, out var reason) || !Enum.IsDefined(reason))
                throw new BusinessException("Geçersiz bildirim nedeni.");

            var clip = await _context.VideoClips
                .FirstOrDefaultAsync(x => x.Id == request.ClipId && x.IsPublished, cancellationToken)
                ?? throw new NotFoundException(nameof(VideoClip), request.ClipId);

            var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
            if (note is { Length: > 500 }) note = note[..500];

            var existing = await _context.ClipReports
                .FirstOrDefaultAsync(x => x.ClipId == clip.Id && x.UserId == request.UserId, cancellationToken);

            if (existing is null)
            {
                await _context.ClipReports.AddAsync(new ClipReport
                {
                    ClipId = clip.Id,
                    UserId = request.UserId,
                    Reason = reason,
                    Note = note,
                }, cancellationToken);
            }
            else
            {
                existing.Reason = reason;
                existing.Note = note;
                existing.Status = "Open";
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
