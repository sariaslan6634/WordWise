using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Common.Models;
using WordWise.Application.Features.Words.Dtos;

namespace WordWise.Application.Features.Words.Queries.GetAllWords
{
    public record GetAllWordsQuery(
        int Page = 1,
        int PageSize = 20,
        string? SearchText = null
        ) : IRequest<PagedResponse<WordSummaryDto>>;
    public class GetAllWordsQueryHandler(
            IWordWiseDbContext _context,
            IMapper _mapper)
            : IRequestHandler<GetAllWordsQuery, PagedResponse<WordSummaryDto>>
    {
        public async Task<PagedResponse<WordSummaryDto>> Handle(GetAllWordsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Words.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchText))
                query = query.Where(x => x.Text.Contains(request.SearchText));

            var totalCount = await query.CountAsync(cancellationToken);

            var words = await query
                .OrderBy(x => x.Text)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = _mapper.Map<List<WordSummaryDto>>(words);

            return PagedResponse<WordSummaryDto>.Create(items, totalCount, request.Page, request.PageSize);
        }
    }

    public class GetAllWordsQueryValidator : AbstractValidator<GetAllWordsQuery>
    {
        public GetAllWordsQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0).WithMessage("Sayfa numarası 0'dan büyük olmalıdır.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Sayfa boyutu 1 ile 100 arasında olmalıdır.");
        }
    }
}
