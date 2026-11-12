using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Common.Settings;
using WordWise.Application.Features.Clips.Models;
using WordWise.Application.Features.Clips.Services;
using WordWise.Domain.Enums;
using WordWise.Infrastructure.Persistence.Context;
using Xunit;

namespace WordWise.Tests;

public class AutoDiscoveryTests
{
    private const string GoodId = "goodvideo01";
    private const string NoCaptionId = "nocaption01";

    private static WordWiseDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<WordWiseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class FakeDiscovery(params string[] ids) : IVideoDiscoveryService
    {
        public Task<List<DiscoveredVideoInfo>> SearchAsync(string query, int maxResults, string videoDuration, CancellationToken cancellationToken = default) =>
            Task.FromResult(ids.Select(id => new DiscoveredVideoInfo(id, "Title " + id)).ToList());
    }

    private sealed class FakeCaptions : ICaptionService
    {
        public Task<CaptionTrack?> GetCaptionsAsync(string youtubeId, CancellationToken cancellationToken = default)
        {
            if (youtubeId == NoCaptionId) return Task.FromResult<CaptionTrack?>(null);
            var lines = new List<CaptionLine>
            {
                new(10, 3, "Daddy is the best cook in the world."),
                new(40, 3, "I really think that we should leave now."),
                new(75, 3, "It was the beginning of a long journey."),
            };
            return Task.FromResult<CaptionTrack?>(new CaptionTrack("en", false, lines));
        }
    }

    private sealed class FakeDetails : IVideoDetailsService
    {
        public Task<VideoDetails?> GetVideoDetailsAsync(string youtubeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<VideoDetails?>(new VideoDetails(youtubeId, "A clean lesson", "Nothing special", "Channel",
                new List<string> { "english" }, 180, "hd", true, "public", false));
    }

    private static ClipDiscoveryRunner CreateRunner(WordWiseDbContext context, bool autoPublish, bool requireRealTimings = false)
    {
        var captions = new FakeCaptions();
        var details = new FakeDetails();
        return new ClipDiscoveryRunner(
            context,
            new FakeDiscovery(GoodId, NoCaptionId),
            new ClipGenerator(context, captions, details),
            Options.Create(new AutoDiscoverySettings { AutoPublish = autoPublish, AutoPublishRequiresRealTimings = requireRealTimings, MaxVideosPerRun = 3 }),
            NullLogger<ClipDiscoveryRunner>.Instance);
    }

    [Fact]
    public async Task Runner_publishes_passed_clips_records_failures_and_skips_known_videos()
    {
        await using var context = CreateContext();
        var runner = CreateRunner(context, autoPublish: true);

        var first = await runner.RunOnceAsync(default);

        Assert.Equal(2, first.VideosProcessed);
        Assert.Single(first.Failures);
        Assert.True(first.ClipsCreated > 0);
        Assert.Equal(first.ClipsCreated, first.ClipsPublished); // temiz videoda hepsi geçer

        var records = await context.DiscoveredVideos.ToListAsync();
        Assert.Single(records, r => r.Status == "Done" && r.YoutubeId == GoodId);
        Assert.Single(records, r => r.Status == "Failed" && r.YoutubeId == NoCaptionId);

        var clips = await context.VideoClips.ToListAsync();
        Assert.All(clips, c => Assert.Equal(c.QcStatus == ClipQcStatus.Passed, c.IsPublished));

        var second = await runner.RunOnceAsync(default);
        Assert.Equal(0, second.VideosProcessed); // aynı videolar tekrar işlenmez
    }

    [Fact]
    public async Task Runner_leaves_clips_pending_when_auto_publish_is_off()
    {
        await using var context = CreateContext();
        var runner = CreateRunner(context, autoPublish: false);

        var result = await runner.RunOnceAsync(default);

        Assert.True(result.ClipsCreated > 0);
        Assert.Equal(0, result.ClipsPublished);
        Assert.All(await context.VideoClips.ToListAsync(), c => Assert.False(c.IsPublished));
    }

    [Fact]
    public async Task Clips_with_estimated_word_timings_wait_for_admin_when_real_timings_are_required()
    {
        await using var context = CreateContext();
        var runner = CreateRunner(context, autoPublish: true, requireRealTimings: true);

        var result = await runner.RunOnceAsync(default);

        Assert.True(result.ClipsCreated > 0);
        Assert.Equal(0, result.ClipsPublished); // sahte altyazıda kelime zamanları tahmini
        var clips = await context.VideoClips.ToListAsync();
        Assert.All(clips, c => Assert.False(c.IsPublished));
        Assert.All(clips, c => Assert.Equal("Channel", c.SourceChannel));
        Assert.All(clips, c => Assert.Equal("Nothing special", c.SourceDescription));
    }
}
