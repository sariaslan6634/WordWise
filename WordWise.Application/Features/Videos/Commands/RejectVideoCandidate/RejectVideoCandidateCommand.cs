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

namespace WordWise.Application.Features.Videos.Commands.RejectVideoCandidate
{
    public record RejectVideoCandidateCommand(Guid CandidateId) : IRequest;
    public class RejectVideoCandidateCommandHandler(IWordWiseDbContext _context) : IRequestHandler<RejectVideoCandidateCommand>
    {
        public async Task Handle(RejectVideoCandidateCommand request, CancellationToken cancellationToken)
        {
            var candidate = await _context.VideoCandidates.FirstOrDefaultAsync(x => x.Id == request.CandidateId, cancellationToken);

            if (candidate is null)
                throw new NotFoundException(nameof(VideoCandidate), request.CandidateId);

            if (candidate.IsApproved)
                throw new BusinessException("Onaylanan adaylar tekrar reddedilemez.");
            candidate.IsRejected = true;

            await _context.SaveChangesAsync(cancellationToken);

        }

    }
}
