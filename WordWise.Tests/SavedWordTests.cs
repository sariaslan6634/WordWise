using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Features.UserWords.Commands.SaveWord;
using WordWise.Application.Features.UserWords.Commands.UnsaveWord;
using WordWise.Application.Features.UserWords.Queries.GetSavedWords;
using WordWise.Domain.Entities;
using WordWise.Infrastructure.Persistence.Context;
using Xunit;

namespace WordWise.Tests;

public class SavedWordTests
{
    private static WordWiseDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<WordWiseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Word PublishedWord() => new()
    {
        Text = "beautiful",
        Definition = "Pleasing the senses.",
        ExampleSentencesJson = "[\"What a beautiful day.\",\"Another one.\"]",
        IsPublished = true,
        IsApproved = true,
    };

    [Fact]
    public async Task Saving_twice_conflicts_and_saving_again_after_unsave_restores_the_same_row()
    {
        await using var context = CreateContext();
        var word = PublishedWord();
        context.Words.Add(word);
        await context.SaveChangesAsync();
        var user = Guid.NewGuid();
        var save = new SaveWordCommandHandler(context);

        var firstId = await save.Handle(new SaveWordCommand(user, word.Id), default);
        await Assert.ThrowsAsync<ConflictException>(() => save.Handle(new SaveWordCommand(user, word.Id), default));

        await new UnsaveWordCommandHandler(context).Handle(new UnsaveWordCommand(user, word.Id), default);
        var secondId = await save.Handle(new SaveWordCommand(user, word.Id), default);

        Assert.Equal(firstId, secondId);
        Assert.Equal(1, await context.UserWords.IgnoreQueryFilters().CountAsync());
        Assert.Equal(1, await context.UserWords.CountAsync());
    }

    [Fact]
    public async Task Saved_word_list_includes_the_first_example_sentence()
    {
        await using var context = CreateContext();
        var word = PublishedWord();
        context.Words.Add(word);
        await context.SaveChangesAsync();
        var user = Guid.NewGuid();
        await new SaveWordCommandHandler(context).Handle(new SaveWordCommand(user, word.Id), default);

        var page = await new GetSavedWordsQueryHandler(context).Handle(new GetSavedWordsQuery(user), default);

        var item = Assert.Single(page.Items);
        Assert.Equal("beautiful", item.Text);
        Assert.Equal("Pleasing the senses.", item.Definition);
        Assert.Equal("What a beautiful day.", item.ExampleSentence);
    }
}
