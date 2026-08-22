using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WordWise.Application.Common.Models;
using WordWise.Application.Features.Admin.Dtos;
using WordWise.Application.Features.Admin.Queries.GetDashboardStats;
using WordWise.Application.Features.Admin.Queries.GetUserList;

namespace WordWise.WebAPI.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController(IMediator _mediator) : ControllerBase
    {
        [HttpGet("stats")]
        [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetDashboardStatsQuery(), cancellationToken);
            return Ok(ApiResponse<DashboardStatsDto>.Ok(result, "Dashboard statistics retrieved successfully."));
        }

        [HttpGet("users")]
        [ProducesResponseType(typeof(ApiResponse<AdminUserListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchText = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(new GetUserListQuery(page, pageSize, searchText), cancellationToken);
            return Ok(ApiResponse<PagedResponse<AdminUserListDto>>.Ok(result));
        }
    }
}
