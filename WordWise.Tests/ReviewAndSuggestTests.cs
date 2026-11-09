using Microsoft.EntityFrameworkCore;
using WordWise.Application.Features.UserWords.Commands.ReviewWord;
using WordWise.Application.Features.UserWords.Commands.SaveWord;
using WordWise.Application.Features.Words.Queries.SuggestWords;
using WordWise.Domain.Entities;
using WordWise.Infrastructure.Persistence.Context;
using Xunit;

namespace WordWise.Tests;

public class ReviewAndSuggestTests
{
    private static WordWiseDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<WordWiseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void Schedule_moves_up_one_level_with_growing_intervals_and_drops_back_when_unknown()
    {
        var now = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal((1, now.AddDays(2)), ReviewWordCommandHandler.Schedule(0, true, now));
        Assert.Equal((5, now.AddDays(32)), ReviewWordCommandHandler.Schedule(4, true, now));
        Assert.Equal((5, now.AddDays(32)), ReviewWordCommandHandler.Schedule(5, true, now));
        Assert.Equal((2, now.AddMinutes(10)), ReviewWordCommandHandler.Schedule(3, false, now));
        Assert.Equal((0, now.AddMinutes(10)), ReviewWordCommandHandler.Schedule(0, false, now));
    }

    [Fact]
    public async Task Reviewing_a_saved_word_updates_level_count_and_next_review()
    {
        await using var context = CreateContext();
        var word = new Word { Text = "car", Definition = "A vehicle.", IsPublished = true };
        context.Words.Add(word);
        await context.SaveChangesAsync();
        var user = Guid.NewGuid();
        await new SaveWordCommandHandler(context).Handle(new SaveWordCommand(user, word.Id), default);

        var result = await new ReviewWordCommandHandler(context).Handle(new ReviewWordCommand(user, word.Id, true), default);

        var saved = await context.UserWords.SingleAsync();
        Assert.Equal(1, result.KnownLevel);
        Assert.Equal(1, saved.KnownLevel);
        Assert.Equal(1, saved.ReviewCount);
        Assert.True(saved.NextReviewAt > DateTime.UtcNow.AddDays(1));
    }

    [Fact]
    public async Task Suggestions_return_only_published_words_starting_with_the_prefix_shortest_first()
    {
        await using var context = CreateContext();
        context.Words.AddRange(
            new Word { Text = "carbohydrate", Definition = "x", IsPublished = true },
            new Word { Text = "car", Definition = "x", IsPublished = true },
            new Word { Text = "card", Definition = "x", IsPublished = true },
            new Word { Text = "cargo", Definition = "x", IsPublished = false },
            new Word { Text = "dog", Definition = "x", IsPublished = true });
        await context.SaveChangesAsync();
        var handler = new SuggestWordsQueryHandler(context);

        var result = await handler.Handle(new SuggestWordsQuery("C", 2), default);

        Assert.Equal(new[] { "car", "card" }, result.Select(x => x.Text).ToArray());
        Assert.Empty(await handler.Handle(new SuggestWordsQuery("%_", 8), default));
    }
}
