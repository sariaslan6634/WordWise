using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Common.Settings;
using WordWise.Application.Features.Auth.Dtos;

namespace WordWise.Application.Features.Auth.Commands.Login
{
    public record LoginCommand(string Email,
        string Password) : IRequest<AuthResponseDto>;

    public class LogincommandHandle(
        IWordWiseDbContext _context,
        IJwtService _jwtService,
        IOptions<JwtSettings> _jwtSettings) : IRequestHandler<LoginCommand, AuthResponseDto>
    {
        public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.Email.ToLower().Trim(), cancellationToken);

            if(user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedException("Gecersiz eposta veya şifre.");

            var token = _jwtService.GenerateToken(user.Id, user.Email, user.Role);

            return new AuthResponseDto 
            {
                UserId = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role.ToString(),
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.Value.ExpiryMinutes)
            };
        }
    }
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-posta alanı zorunludur.")
                .EmailAddress().WithMessage("Geçersiz e-posta formatı.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre alanı zorunludur.");
        }
    }
}
