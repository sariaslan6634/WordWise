using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Videos.Commands.ApproveVideoCandidate
{
    public record ApproveVideoCandidateCommand(
        Guid CandidateId,
        Guid ApprovedByUserId,
        int StartSec,
        int EndSec,
        string? Transcript) : IRequest<Guid>;
    public class ApproveVideoCandidateCommandHandler(IWordWiseDbContext _context) : IRequestHandler<ApproveVideoCandidateCommand, Guid>
    {
        public async Task<Guid> Handle(ApproveVideoCandidateCommand request, CancellationToken cancellationToken)
        {
            var candidate = await _context.VideoCandidates.Include(x => x.Word).FirstOrDefaultAsync(x => x.Id == request.CandidateId, cancellationToken);

            if(candidate is null)
                throw new NotFoundException(nameof(VideoCandidate), request.CandidateId);

            if (candidate.IsApproved)
                throw new BusinessException("Bu aday zaten onaylanmış");

            if (candidate.IsRejected)
                throw new BusinessException("Reddedilen adaylar onaylanamaz.");

            var alreadyApproved = await _context.Videos.AnyAsync(x => x.WordId == candidate.WordId && x.YoutubeId == candidate.YoutubeId, cancellationToken);

            if (alreadyApproved)
                throw new ConflictException("Video", "Bu YouTube videosu bu kelime için zaten onaylanmış.");

            var maxOrder = await _context.Videos.Where(x => x.WordId == candidate.WordId).MaxAsync(x => (int?)x.DisplayOrder, cancellationToken) ?? 0;

            var video = new Video
            {
                WordId = candidate.WordId,
                YoutubeId = candidate.YoutubeId,
                Title = candidate.Title,
                ChannelTitle = candidate.ChannelTitle,
                ThumbnailUrl = candidate.ThumbnailUrl,
                StartSec = request.StartSec,
                EndSec = request.EndSec,
                DisplayOrder = maxOrder + 1,
                Transcript = request.Transcript,
                IsPublished = true,
                SourceCandidateId = candidate.Id,
            };
            await _context.Videos.AddAsync(video, cancellationToken);

            candidate.IsApproved = true;
            candidate.ApprovedByUserId = request.ApprovedByUserId;
            candidate.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return video.Id;
        }
    }
    public class ApproveVideoCandidateCommandValidator : AbstractValidator<ApproveVideoCandidateCommand>
    {
        public ApproveVideoCandidateCommandValidator()
        {
            RuleFor(x => x.CandidateId)
                .NotEmpty().WithMessage("CandidateId alanı zorunludur.");

            RuleFor(x => x.ApprovedByUserId)
                .NotEmpty().WithMessage("ApprovedByUserId alanı zorunludur.");

            RuleFor(x => x.StartSec)
                .GreaterThanOrEqualTo(0).WithMessage("Başlangıç değeri 0 veya daha büyük olmalıdır.");

            RuleFor(x => x.EndSec)
                .GreaterThan(x => x.StartSec).WithMessage("Bitiş değeri başlangıç değerinden büyük olmalıdır.");
        }
    }
}
