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
using WordWise.Application.Features.UserWords.Dtos;

namespace WordWise.Application.Features.UserWords.Queries.GetSavedWords
{
    public record GetSavedWordsQuery(Guid UserId, int Page = 1, int PageSize = 20) : IRequest<PagedResponse<SavedWordDto>>;

    public class GetSavedWordsQueryHandler(IWordWiseDbContext _context) : IRequestHandler<GetSavedWordsQuery, PagedResponse<SavedWordDto>>
    {
        public async Task<PagedResponse<SavedWordDto>> Handle(GetSavedWordsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.UserWords.Include(x => x.Word).Where(x => x.UserId == request.UserId);

            var totalCount = await query.CountAsync(cancellationToken);

            var userWords = await query
                  .OrderByDescending(x => x.CreatedAt)
                  .Skip((request.Page - 1) * request.PageSize)
                  .Take(request.PageSize)
                  .ToListAsync(cancellationToken);

            var items = userWords.Select(uw => new SavedWordDto
            {
                Id = uw.Id,
                WordId = uw.WordId,
                Text = uw.Word.Text,
                Definition = uw.Word.Definition,
                PartOfSpeech = uw.Word.PartOfSpeech,
                CefrLevel = uw.Word.CefrLevel?.ToString(),
                KnownLevel = uw.KnownLevel,
                NextReviewAt = uw.NextReviewAt,
                ReviewCount = uw.ReviewCount,
                PersonalNote = uw.PersonalNote,
                SavedAt = uw.CreatedAt,
            }).ToList();

            return PagedResponse<SavedWordDto>.Create(items, totalCount, request.Page, request.PageSize);
        }
    }
    public class GetSavedWordsQueryValidator : AbstractValidator<GetSavedWordsQuery>
    {
        public GetSavedWordsQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Kullanıcı kimliği (UserId) zorunludur.");

            RuleFor(x => x.Page)
                .GreaterThan(0).WithMessage("Sayfa numarası 0'dan büyük olmalıdır.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Sayfa boyutu 1 ile 100 arasında olmalıdır.");
        }
    }


}
