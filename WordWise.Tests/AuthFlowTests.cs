using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Common.Settings;
using WordWise.Application.Features.Auth.Commands.Login;
using WordWise.Application.Features.Auth.Commands.Register;
using WordWise.Application.Features.Quizzes.Commands.SubmitQuizAnswer;
using WordWise.Domain.Entities;
using WordWise.Domain.Enums;
using WordWise.Infrastructure.Persistence.Context;
using WordWise.Infrastructure.Services.Email;

namespace WordWise.Tests;

public class AuthFlowTests
{
    [Fact]
    public void Email_confirmation_token_verifies_against_its_hash()
    {
        var service = new EmailConfirmationService();
        var token = service.GenerateToken();
        var hash = service.HashToken(token);

        Assert.True(service.VerifyToken(token, hash));
        Assert.False(service.VerifyToken("different-token", hash));
        Assert.False(service.VerifyToken(token, "not-a-valid-hash"));
    }

    [Fact]
    public async Task Register_persists_hashed_email_confirmation_token_with_expiry()
    {
        await using var context = CreateContext();
        var confirmation = new FakeConfirmationService();
        var handler = new RegisterCommandHandler(
            context,
            new FakeJwtService(),
            new FakeEmailSender(),
            confirmation,
            Options.Create(new JwtSettings { ExpiryMinutes = 60 }),
            Options.Create(new EmailSettings { AppBaseUrl = "https://example.test" }));

        await handler.Handle(new RegisterCommand("  USER@example.com ", " Test User ", "secret1", "secret1"), default);

        var user = await context.Users.SingleAsync();
        Assert.Equal("user@example.com", user.Email);
        Assert.False(user.IsEmailConfirmed);
        Assert.Equal(confirmation.HashToken("raw-confirmation-token"), user.EmailConfirmationTokenHash);
        Assert.InRange(user.EmailConfirmationTokenExpiresAt!.Value, DateTime.UtcNow.AddHours(23), DateTime.UtcNow.AddHours(25));
    }

    [Fact]
    public async Task Login_rejects_an_unconfirmed_email()
    {
        await using var context = CreateContext();
        context.Users.Add(new WordWise.Domain.Entities.User
        {
            Email = "user@example.com",
            Name = "User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret1"),
            IsEmailConfirmed = false
        });
        await context.SaveChangesAsync();
        var handler = new LogincommandHandle(context, new FakeJwtService(), Options.Create(new JwtSettings()));

        await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(
            new LoginCommand("user@example.com", "secret1"), default));
    }

    [Fact]
    public async Task Login_returns_token_for_confirmed_user_with_valid_password()
    {
        await using var context = CreateContext();
        var user = new WordWise.Domain.Entities.User
        {
            Email = "user@example.com",
            Name = "User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret1"),
            IsEmailConfirmed = true,
            Role = UserRole.User
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var handler = new LogincommandHandle(context, new FakeJwtService(), Options.Create(new JwtSettings { ExpiryMinutes = 30 }));

        var result = await handler.Handle(new LoginCommand("user@example.com", "secret1"), default);

        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("test-token", result.Token);
    }

    [Fact]
    public async Task Submit_quiz_answer_rejects_unpublished_question()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.com", Name = "User" };
        var question = new QuizQuestion { QuestionText = "Question", CorrectAnswer = "yes", IsPublished = false };
        context.Users.Add(user);
        context.QuizQuestions.Add(question);
        await context.SaveChangesAsync();
        var handler = new SubmitQuizAnswerCommandHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new SubmitQuizAnswerCommand(user.Id, question.Id, "yes"), default));
    }

    [Fact]
    public async Task Submit_quiz_answer_awards_xp_only_once_per_24_hours()
    {
        await using var context = CreateContext();
        var user = new User { Email = "user@example.com", Name = "User" };
        var question = new QuizQuestion { QuestionText = "Question", CorrectAnswer = "yes", IsPublished = true };
        context.Users.Add(user);
        context.QuizQuestions.Add(question);
        await context.SaveChangesAsync();
        var handler = new SubmitQuizAnswerCommandHandler(context);

        var first = await handler.Handle(new SubmitQuizAnswerCommand(user.Id, question.Id, " YES "), default);
        var second = await handler.Handle(new SubmitQuizAnswerCommand(user.Id, question.Id, "yes"), default);

        Assert.True(first.XpAwarded);
        Assert.Equal(10, first.XpChange);
        Assert.False(second.XpAwarded);
        Assert.Equal(0, second.XpChange);
        Assert.Equal(10, second.TotalXp);
        Assert.Equal(2, await context.UserQuizAnswers.CountAsync());
        Assert.Single(await context.UserXpHistories.ToListAsync());
    }

    private static WordWiseDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WordWiseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new WordWiseDbContext(options);
    }

    private sealed class FakeConfirmationService : IEmailConfirmationService
    {
        public string GenerateToken() => "raw-confirmation-token";
        public string HashToken(string token) => $"hashed:{token}";
        public bool VerifyToken(string token, string storedHash) => storedHash == HashToken(token);
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string Subject, string htmlBody, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeJwtService : IJwtService
    {
        public string GenerateToken(Guid Id, string email, UserRole role) => "test-token";
    }
}
