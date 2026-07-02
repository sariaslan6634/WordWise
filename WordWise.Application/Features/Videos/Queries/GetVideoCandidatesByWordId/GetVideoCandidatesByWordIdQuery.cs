using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Videos.Dtos;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Videos.Queries.GetVideoCandidatesByWordId
{
    public record GetVideoCandidatesByWordIdQuery(Guid WordId)
    : IRequest<List<VideoCandidateDto>>;
    public class GetVideoCandidatesByWordIdQueryHandler(
            IWordWiseDbContext _context,
            IMapper _mapper)
            : IRequestHandler<GetVideoCandidatesByWordIdQuery, List<VideoCandidateDto>>
    {
        public async Task<List<VideoCandidateDto>> Handle(GetVideoCandidatesByWordIdQuery request, CancellationToken cancellationToken)
        {
            var candidates = await _context.VideoCandidates.Include(x => x.Word)
                .Where(x => x.WordId == request.WordId)
                .OrderByDescending(x => x.FetchedAt)
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<VideoCandidateDto>>(candidates);
        }
    }
}
