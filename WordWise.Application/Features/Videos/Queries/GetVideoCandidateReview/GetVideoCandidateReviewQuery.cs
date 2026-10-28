using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Videos.Dtos;
using WordWise.Application.Features.Words.Dtos;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Videos.Queries.GetVideoCandidateReview;

public record GetVideoCandidateReviewQuery(Guid CandidateId) : IRequest<VideoCandidateReviewDto>;

public sealed class GetVideoCandidateReviewQueryHandler(IWordWiseDbContext context, IMapper mapper)
    : IRequestHandler<GetVideoCandidateReviewQuery, VideoCandidateReviewDto>
{
    public async Task<VideoCandidateReviewDto> Handle(GetVideoCandidateReviewQuery request, CancellationToken cancellationToken)
    {
        var candidate = await context.VideoCandidates
            .Include(item => item.Word)
            .FirstOrDefaultAsync(item => item.Id == request.CandidateId, cancellationToken);

        if (candidate is null || candidate.IsApproved || candidate.IsRejected)
            throw new NotFoundException(nameof(VideoCandidate), request.CandidateId);

        return new VideoCandidateReviewDto
        {
            Candidate = mapper.Map<VideoCandidateDto>(candidate),
            Word = mapper.Map<WordDetailDto>(candidate.Word),
        };
    }
}
