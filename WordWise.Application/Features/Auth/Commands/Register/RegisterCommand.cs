using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Common.Settings;
using WordWise.Application.Features.Auth.Dtos;
using WordWise.Domain.Entities;
using WordWise.Domain.Enums;

namespace WordWise.Application.Features.Auth.Commands.Register
{
    public record RegisterCommand(
        string Email,
        string Name,
        string Password,
        string ConfirmPassword) : IRequest<AuthResponseDto>;
    public class RegisterCommandHandler
        (IWordWiseDbContext _context, 
        IJwtService _jwtService,
        IEmailSender _emailSender,
        IEmailConfirmationService _confirmationService,
        IOptions<JwtSettings> _jwtSettings,
        IOptions<EmailSettings> _emailSettings) : IRequestHandler<RegisterCommand, AuthResponseDto>
    {
        public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var emailExist = await _context.Users.AnyAsync(x => x.Email == request.Email.ToLower().Trim(), cancellationToken);
            if (emailExist)
                throw new ConflictException("User", $"Email '{request.Email}'zaten kayıtlı.");

            var rawToken = _confirmationService.GenerateToken();
            var tokenHash = _confirmationService.HashToken(rawToken);


            var user = new User
            {
                Email = request.Email.ToLower().Trim(),
                Name = request.Name.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = UserRole.User
            };

            await _context.Users.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var confirmLink =
                $"{_emailSettings.Value.AppBaseUrl}/api/auth/confirm-email" +
                $"?userId={user.Id}&token={Uri.EscapeDataString(rawToken)}";

            var htmlBody = $@"
                <h2>Welcome to WordWise, {user.Name}!</h2>
                <p>Lütfen aşşağıdaki bağlantıya tıklayarak e-posta adresini onayla:</p>
                <p><a href=""{confirmLink}"">E-postamı onayla</a></p>
                <p>Bu bağlantının süresi 24 saat içinde dolacaktır.</p>";

            await _emailSender.SendAsync(user.Email,
                "E-posta adresiniz onaylandı.",
                htmlBody,
                cancellationToken
                );

            var token = _jwtService.GenerateToken(user.Id, user.Email, user.Role);
            return new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role.ToString(),
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.Value.Expiryminutes)
            };
        }
    }

    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta alanı zorunludur.")
            .EmailAddress().WithMessage("Geçersiz e-posta formatı.")
            .MaximumLength(256).WithMessage("E-posta adresi en fazla 256 karakter olmalıdır.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("İsim alanı zorunludur.")
                .MinimumLength(2).WithMessage("İsim en az 2 karakter olmalıdır.")
                .MaximumLength(100).WithMessage("İsim en fazla 100 karakter olmalıdır.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre alanı zorunludur.")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.")
                .MaximumLength(100).WithMessage("Şifre en fazla 100 karakter olmalıdır.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Şifre tekrar alanı zorunludur.")
                .Equal(x => x.Password).WithMessage("Şifreler eşleşmiyor.");
        }
    }
}
