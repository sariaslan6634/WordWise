using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Domain.Entities;

namespace WordWise.Application.Features.Auth.Commands.ConfirmEmail
{
    public record ConfirmEmailCommand(
        Guid UserId,
        string Token) : IRequest<bool>;
    public class ConfirmEmailCommandhandler(IWordWiseDbContext _context, IEmailConfirmationService _confirmService) : IRequestHandler<ConfirmEmailCommand, bool>
    {
        public async Task<bool> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

            if (user is null)
                throw new NotFoundException(nameof(User),request.UserId);

            if(user.IsEmailConfirmed) 
                throw new BusinessException("Bu e-posta adresi zaten onaylanmış.");

            if(user.EmailConfirmationTokenHash is null || user.EmailConfirmationTokenExpiresAt is null)
                throw new BusinessException("Bu hesap için bekleyen bir onay işlemi bulunamadı.");
            if (user.EmailConfirmationTokenExpiresAt < DateTime.UtcNow)
                throw new BusinessException("Onay bağlantısının süresi dolmuş. Lütfen yeni bir bağlantı talep edin.");
            var isValid = _confirmService.VerifyToken(
            request.Token, user.EmailConfirmationTokenHash);

            if (!isValid)
                throw new BusinessException("Geçersiz onay bağlantısı.");

            user.IsEmailConfirmed = true;
            user.EmailConfirmedAt = DateTime.UtcNow;
            user.EmailConfirmationTokenHash = null;
            user.EmailConfirmationTokenExpiresAt = null;

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }

    public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
    {
        public ConfirmEmailCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId zorunludur.");
            RuleFor(x => x.Token).NotEmpty().WithMessage("Token zorunludur.");
        }
    }

}
