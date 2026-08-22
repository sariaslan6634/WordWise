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
using WordWise.Application.Features.Admin.Dtos;

namespace WordWise.Application.Features.Admin.Queries.GetUserList
{
    public record GetUserListQuery(int Page = 1, int PageSize = 20, string? SearchText = null) : IRequest<PagedResponse<AdminUserListDto>>;
    public class GetUserListQueryHandler(IWordWiseDbContext _context) : IRequestHandler<GetUserListQuery, PagedResponse<AdminUserListDto>>
    {
        public async Task<PagedResponse<AdminUserListDto>> Handle(GetUserListQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                query = query.Where(x => x.Name.Contains(request.SearchText) || x.Email.Contains(request.SearchText));
            }
            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query.OrderByDescending(x => x.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var userIds = users.Select(x => x.Id).ToList();

            var xpData = await _context.UserXpHistories.Where(x=> userIds.Contains(x.UserId))
                .GroupBy(x=>x.UserId)
                .Select(g => new 
                {
                    UserId = g.Key,
                    TotalXp = g.OrderByDescending(x=>x.CreatedAt).Select(x=>x.TotalXpAfterChange).FirstOrDefault()
                }).ToListAsync(cancellationToken);

            var lastActiveData = await _context.UserQuizAnswers
                .Where(x => userIds.Contains(x.UserId))
                .GroupBy(x => x.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    LastActiveAt = g.Max(x => x.AnsweredAt)
                })
                .ToListAsync(cancellationToken);

            var items = users.Select(user =>
            {
                var xp = xpData.FirstOrDefault(x => x.UserId == user.Id);
                var lastActive = lastActiveData.FirstOrDefault(x => x.UserId == user.Id);

                return new AdminUserListDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    TotalXp = xp?.TotalXp ?? 0,
                    MemberSince = user.CreatedAt,
                    LastActiveAt = lastActive?.LastActiveAt

                };
            }).ToList();

            return PagedResponse<AdminUserListDto>.Create(items, totalCount, request.Page, request.PageSize);
        }
    }
    public class GetUserListQueryValidator : AbstractValidator<GetUserListQuery>
    {
        public GetUserListQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0).WithMessage("Page must be greater than 0.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        }
    }
}
