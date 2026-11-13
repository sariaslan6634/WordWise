using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Features.Clips.Models;
using WordWise.Application.Features.Clips.Queries.GetClipsFromVideo;
using WordWise.Application.Features.Clips.Services;
using WordWise.Infrastructure.Persistence.Context;
using Xunit;

namespace WordWise.Tests;

public class ClipsFromVideoTests
{
    private static WordWiseDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<WordWiseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class FakeCaptions : ICaptionService
    {
        public Task<CaptionTrack?> GetCaptionsAsync(string youtubeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CaptionTrack?>(new CaptionTrack("en", false, new List<CaptionLine>
            {
                new(10, 3, "Daddy is the best cook in the world."),
                new(40, 3, "I really think that we should leave now."),
                new(75, 3, "It was the beginning of a long journey."),
            }));
    }

    private sealed class FakeDetails : IVideoDetailsService
    {
        public Task<VideoDetails?> GetVideoDetailsAsync(string youtubeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<VideoDetails?>(new VideoDetails(youtubeId, "A clean lesson", "Nothing special", "Channel",
                new List<string> { "english" }, 180, "hd", true, "public", false));
    }

    [Fact]
    public async Task Invalid_link_is_rejected()
    {
        await using var context = CreateContext();
        var handler = new GetClipsFromVideoQueryHandler(context, new ClipGenerator(context, new FakeCaptions(), new FakeDetails()));

        await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(new GetClipsFromVideoQuery(Guid.NewGuid(), "not a link"), default));
    }

    [Fact]
    public async Task Clips_are_generated_unpublished_and_reused_on_the_next_request()
    {
        await using var context = CreateContext();
        var handler = new GetClipsFromVideoQueryHandler(context, new ClipGenerator(context, new FakeCaptions(), new FakeDetails()));
        var user = Guid.NewGuid();

        var first = await handler.Handle(new GetClipsFromVideoQuery(user, "https://www.youtube.com/watch?v=goodvideo01"), default);

        Assert.NotEmpty(first);
        Assert.All(first, c => Assert.Equal("goodvideo01", c.YoutubeId));
        Assert.All(await context.VideoClips.ToListAsync(), c => Assert.False(c.IsPublished)); // başkalarına gösterilmez

        var second = await handler.Handle(new GetClipsFromVideoQuery(Guid.NewGuid(), "https://youtu.be/goodvideo01"), default);
        Assert.Equal(first.Select(c => c.Id).OrderBy(x => x), second.Select(c => c.Id).OrderBy(x => x)); // yeniden üretilmez
    }
}
