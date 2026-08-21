using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WordWise.Application.Common.Models;
using WordWise.Application.Features.UserProfile.Dtos;
using WordWise.Application.Features.UserProfile.Queries.GetUserProfile;

namespace WordWise.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController(IMediator _mediator) : ControllerBase
    {
        private Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetUserProfileQuery(CurrentUserId), cancellationToken);

            return Ok(ApiResponse<UserProfileDto>.Ok(result, "Profil başarılı bir şekilde alındı"));
        }
    }
}
