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
using WordWise.Domain.Enums;

namespace WordWise.Application.Features.Words.Queries.GetAllWords
{
    public record GetAllWordsQuery(
        int Page = 1,
        int PageSize = 20,
        string? SearchText = null,
        bool? IsApproved = null,
        CefrLevel? CefrLevel = null,
        string? PartOfSpeech = null,
        string? Status = null
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
                query = query.Where(x => x.Text.Contains(request.SearchText) || x.Definition.Contains(request.SearchText));
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = request.Status.ToLowerInvariant() switch
                {
                    "active" => query.Where(x => x.IsApproved && x.IsPublished),
                    "passive" => query.Where(x => x.IsApproved && !x.IsPublished),
                    "draft" => query.Where(x => !x.IsApproved),
                    _ => query.Where(x => x.IsApproved)
                };
            }
            else if (request.IsApproved.HasValue)
                query = query.Where(x => x.IsApproved == request.IsApproved.Value);
            if (request.CefrLevel.HasValue)
                query = query.Where(x => x.CefrLevel == request.CefrLevel.Value);
            if (!string.IsNullOrWhiteSpace(request.PartOfSpeech))
                query = query.Where(x => x.PartOfSpeech == request.PartOfSpeech);

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
