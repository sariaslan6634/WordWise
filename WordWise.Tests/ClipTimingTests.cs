using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Features.Clips.Commands.ReviewClip;
using WordWise.Domain.Entities;
using WordWise.Domain.Enums;
using WordWise.Infrastructure.Persistence.Context;
using Xunit;

namespace WordWise.Tests;

public class ClipTimingTests
{
    private static WordWiseDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<WordWiseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static VideoClip Clip(bool published) => new()
    {
        YoutubeId = "abcdefghijk",
        StartSec = 10,
        EndSec = 13,
        Text = "This is perfect for me.",
        QcStatus = ClipQcStatus.Passed,
        IsPublished = published,
    };

    [Fact]
    public async Task Pending_clip_timing_can_be_adjusted()
    {
        await using var context = CreateContext();
        var clip = Clip(published: false);
        context.VideoClips.Add(clip);
        await context.SaveChangesAsync();

        var result = await new AdjustClipTimingCommandHandler(context).Handle(new AdjustClipTimingCommand(clip.Id, 9.5, 15.255), default);

        Assert.Equal(9.5, result.StartSec);
        Assert.Equal(15.26, result.EndSec); // 2 basamağa yuvarlanır
        Assert.Equal(15.26, (await context.VideoClips.SingleAsync()).EndSec);
    }

    [Fact]
    public async Task Published_clip_timing_cannot_be_changed()
    {
        await using var context = CreateContext();
        var clip = Clip(published: true);
        context.VideoClips.Add(clip);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessException>(() =>
            new AdjustClipTimingCommandHandler(context).Handle(new AdjustClipTimingCommand(clip.Id, 10, 16), default));
        Assert.Equal(13, (await context.VideoClips.SingleAsync()).EndSec);
    }

    [Theory]
    [InlineData(-1, 5)]
    [InlineData(10, 10)]
    [InlineData(10, 8)]
    [InlineData(0, 61)]
    public async Task Invalid_ranges_are_rejected(double start, double end)
    {
        await using var context = CreateContext();
        var clip = Clip(published: false);
        context.VideoClips.Add(clip);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessException>(() =>
            new AdjustClipTimingCommandHandler(context).Handle(new AdjustClipTimingCommand(clip.Id, start, end), default));
    }

    [Fact]
    public async Task Unknown_clip_is_not_found()
    {
        await using var context = CreateContext();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new AdjustClipTimingCommandHandler(context).Handle(new AdjustClipTimingCommand(Guid.NewGuid(), 1, 2), default));
    }
}
