using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WordWise.Application.Common.Models;
using WordWise.Application.Features.Auth.Commands.ConfirmEmail;
using WordWise.Application.Features.Auth.Commands.Login;
using WordWise.Application.Features.Auth.Commands.Register;
using WordWise.Application.Features.Auth.Dtos;

namespace WordWise.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthsController(IMediator _mediator) : ControllerBase
    {
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(
            [FromBody] RegisterCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return StatusCode(201, ApiResponse<AuthResponseDto>.Created(result, "Kayıt başarılı."));
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(
            [FromBody] LoginCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Giriş başarılı."));
        }

        [HttpGet("confirm-email")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConfirmEmail(
            [FromQuery] Guid userId,
            [FromQuery] string token,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(new ConfirmEmailCommand(userId, token), cancellationToken);
            return Ok(ApiResponse.OkNoData("Email Adresiniz başarılı bir şekilde onaylanmıştır.Şimdi giriş yapabilirsiniz."));
        }


    }
}
