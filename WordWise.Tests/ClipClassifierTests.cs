using Microsoft.EntityFrameworkCore;
using WordWise.Application.Features.Clips.Queries.GetLearnerClips;
using WordWise.Application.Features.Clips.Services;
using WordWise.Domain.Entities;
using WordWise.Domain.Enums;
using WordWise.Infrastructure.Persistence.Context;
using Xunit;

namespace WordWise.Tests;

public class ClipClassifierTests
{
    [Theory]
    [InlineData("Peter Piper picked a peck of pickled peppers.", "Fun lesson", ClipCategory.TongueTwister)]
    [InlineData("She sells seashells by the seashore.", null, ClipCategory.TongueTwister)]
    [InlineData("Hello there.", "10 Tongue Twisters for kids", ClipCategory.TongueTwister)]
    [InlineData("Hello, is it me you're looking for?", "Lionel Richie - Hello (Official Music Video)", ClipCategory.Song)]
    [InlineData("Thank you.", "Best song lyrics", ClipCategory.Song)]
    [InlineData("How are you?", "English conversation", ClipCategory.SetPhrase)]
    [InlineData("Nice to meet you, I'm Tom.", "English conversation", ClipCategory.SetPhrase)]
    [InlineData("It's raining cats and dogs outside today.", null, ClipCategory.SetPhrase)]
    [InlineData("I think that they tell the truth to them.", "English conversation", ClipCategory.General)]
    [InlineData("The weather report says it will be sunny tomorrow afternoon.", null, ClipCategory.General)]
    public void Classifies_clips_by_text_and_title(string text, string? title, ClipCategory expected)
    {
        Assert.Equal(expected, ClipClassifier.Classify(text, title));
    }

    [Fact]
    public void Parses_category_filter_from_query_string()
    {
        Assert.Null(ClipClassifier.ParseFilter(null));
        Assert.Null(ClipClassifier.ParseFilter("all"));
        Assert.Null(ClipClassifier.ParseFilter("unknown"));
        Assert.Equal(new[] { ClipCategory.Song, ClipCategory.TongueTwister },
            ClipClassifier.ParseFilter("song, twister").OrderBy(c => c).ToArray());
    }

    private static VideoClip Clip(string text, string? title = "English lesson") => new()
    {
        YoutubeId = "abcdefghijk",
        SourceTitle = title,
        StartSec = 1,
        EndSec = 4,
        Text = text,
        WordTimingsJson = "[]",
        QcStatus = ClipQcStatus.Passed,
        IsPublished = true,
    };

    [Fact]
    public async Task Learner_query_filters_by_selected_categories_and_returns_everything_when_none_selected()
    {
        await using var context = new WordWiseDbContext(new DbContextOptionsBuilder<WordWiseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.VideoClips.AddRange(
            Clip("Peter Piper picked a peck of pickled peppers."),
            Clip("How are you?"),
            Clip("The weather report says it will be sunny tomorrow afternoon."),
            Clip("Hello, is it me?", "Artist - Hello (Official Music Video)"));
        await context.SaveChangesAsync();
        var handler = new GetLearnerClipsQueryHandler(context);
        var user = Guid.NewGuid();

        var all = await handler.Handle(new GetLearnerClipsQuery(user, 20), default);
        var twisters = await handler.Handle(new GetLearnerClipsQuery(user, 20, new[] { ClipCategory.TongueTwister }), default);
        var mixed = await handler.Handle(new GetLearnerClipsQuery(user, 20, new[] { ClipCategory.Song, ClipCategory.SetPhrase }), default);

        Assert.Equal(4, all.Count);
        Assert.Equal("twister", Assert.Single(twisters).Category);
        Assert.Equal(new[] { "phrase", "song" }, mixed.Select(c => c.Category).OrderBy(c => c).ToArray());
    }
}
