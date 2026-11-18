using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Common.Settings;
using WordWise.Application.Features.Clips.Services;
using WordWise.Domain.Entities;
using WordWise.Infrastructure.Persistence.Context;
using Xunit;

namespace WordWise.Tests;

public class DiscoveryRunQuotaTests
{
    private static readonly DateTime Now = new(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);

    private static WordWiseDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<WordWiseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static DiscoveryRunQuota Quota(WordWiseDbContext context) =>
        new(context, Options.Create(new AutoDiscoverySettings())); // 5 hak, 1 yedek, 24 saat

    [Fact]
    public async Task Four_clicks_are_allowed_and_one_is_always_kept_in_reserve()
    {
        await using var context = CreateContext();
        var quota = Quota(context);

        var fresh = await quota.GetStatusAsync(Now, default);
        Assert.Equal(4, fresh.AllowedPerWindow);
        Assert.Equal(4, fresh.Remaining);
        Assert.Null(fresh.NextAvailableAt);

        for (var i = 0; i < 4; i++)
        {
            var log = await quota.ConsumeAsync(Now.AddMinutes(i), default);
            await quota.RecordResultAsync(log, videosProcessed: 5, clipsCreated: 40, clipsPublished: 30, failedVideos: 0, default);
        }

        var spent = await quota.GetStatusAsync(Now.AddMinutes(10), default);
        Assert.Equal(0, spent.Remaining);
        Assert.Equal(4, spent.Used);
        Assert.Equal(5, spent.VideosPerClick);
        Assert.Equal(20, spent.VideosInWindow); // penceredeki toplam çekilen video
        Assert.Equal(160, spent.ClipsInWindow);
        Assert.Equal(120, spent.PublishedInWindow);
        Assert.Equal(new DateTimeOffset(Now.AddHours(24)), spent.NextAvailableAt); // ilk tıklama pencereden çıkınca

        var error = await Assert.ThrowsAsync<BusinessException>(() => quota.ConsumeAsync(Now.AddMinutes(10), default));
        Assert.Contains("23 saat 50 dakika", error.Message);
        Assert.Equal(4, await context.DiscoveryRunLogs.CountAsync()); // reddedilen tıklama sayılmaz
    }

    [Fact]
    public async Task Clicks_older_than_the_window_are_freed()
    {
        await using var context = CreateContext();
        var quota = Quota(context);
        for (var i = 0; i < 4; i++) await quota.ConsumeAsync(Now.AddMinutes(i), default);

        var later = await quota.GetStatusAsync(Now.AddHours(24).AddSeconds(30), default); // ilk tıklama süresi doldu
        Assert.Equal(1, later.Remaining);
        Assert.Null(later.NextAvailableAt);
    }
}
