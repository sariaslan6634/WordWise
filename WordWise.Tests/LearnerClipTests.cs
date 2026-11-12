using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Features.Clips.Commands.ReportClip;
using WordWise.Application.Features.Clips.Commands.SaveClip;
using WordWise.Application.Features.Clips.Queries.GetLearnerClips;
using WordWise.Application.Features.Clips.Queries.GetSavedClips;
using WordWise.Domain.Entities;
using WordWise.Domain.Enums;
using WordWise.Infrastructure.Persistence.Context;
using Xunit;

namespace WordWise.Tests;

public class LearnerClipTests
{
    private static WordWiseDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<WordWiseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static VideoClip Clip(bool published, bool rejected = false) => new()
    {
        YoutubeId = "abcdefghijk",
        StartSec = 10,
        EndSec = 13,
        Text = "Daddy is the best cook in the world.",
        WordTimingsJson = "[{\"text\":\"Daddy\",\"start\":10.1,\"end\":10.4}]",
        QcStatus = ClipQcStatus.Passed,
        IsPublished = published,
        IsRejected = rejected,
    };

    [Fact]
    public async Task Learner_query_returns_only_published_clips_and_hides_ones_the_user_reported()
    {
        await using var context = CreateContext();
        var visible = Clip(published: true);
        var reported = Clip(published: true);
        context.VideoClips.AddRange(visible, reported, Clip(published: false), Clip(published: true, rejected: true));
        var user = Guid.NewGuid();
        context.ClipReports.Add(new ClipReport { ClipId = reported.Id, UserId = user, Reason = ClipReportReason.Quality });
        await context.SaveChangesAsync();

        var result = await new GetLearnerClipsQueryHandler(context).Handle(new GetLearnerClipsQuery(user, 20), default);

        var only = Assert.Single(result);
        Assert.Equal(visible.Id, only.Id);
        Assert.Single(only.Words);

        // Başka bir kullanıcı bildirilen klibi de görmeye devam eder
        var other = await new GetLearnerClipsQueryHandler(context).Handle(new GetLearnerClipsQuery(Guid.NewGuid(), 20), default);
        Assert.Equal(2, other.Count);
    }

    [Fact]
    public async Task Report_is_stored_once_per_user_and_updated_on_repeat()
    {
        await using var context = CreateContext();
        var clip = Clip(published: true);
        context.VideoClips.Add(clip);
        await context.SaveChangesAsync();
        var handler = new ReportClipCommandHandler(context);
        var user = Guid.NewGuid();

        await handler.Handle(new ReportClipCommand(user, clip.Id, "quality", null), default);
        await handler.Handle(new ReportClipCommand(user, clip.Id, "Inappropriate", "  kaba dil  "), default);

        var report = Assert.Single(await context.ClipReports.ToListAsync());
        Assert.Equal(ClipReportReason.Inappropriate, report.Reason);
        Assert.Equal("kaba dil", report.Note);
        Assert.Equal("Open", report.Status);
    }

    [Fact]
    public async Task Report_rejects_unknown_reason_and_unpublished_clip()
    {
        await using var context = CreateContext();
        var published = Clip(published: true);
        var hidden = Clip(published: false);
        context.VideoClips.AddRange(published, hidden);
        await context.SaveChangesAsync();
        var handler = new ReportClipCommandHandler(context);

        await Assert.ThrowsAsync<BusinessException>(() =>
            handler.Handle(new ReportClipCommand(Guid.NewGuid(), published.Id, "banana", null), default));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ReportClipCommand(Guid.NewGuid(), hidden.Id, "Quality", null), default));
    }

    [Fact]
    public async Task Saving_clips_is_idempotent_listed_newest_first_and_can_be_restricted_to_in_the_random_feed()
    {
        await using var context = CreateContext();
        var a = Clip(published: true);
        var b = Clip(published: true);
        var unpublished = Clip(published: false);
        context.VideoClips.AddRange(a, b, unpublished);
        await context.SaveChangesAsync();
        var user = Guid.NewGuid();
        var save = new SaveClipCommandHandler(context);

        await save.Handle(new SaveClipCommand(user, a.Id), default);
        await save.Handle(new SaveClipCommand(user, a.Id), default); // tekrar: hata yok, kopya yok
        await save.Handle(new SaveClipCommand(user, unpublished.Id), default); // sonradan yayından kalksa da kaydedilebilir
        Assert.Equal(2, await context.UserClips.CountAsync());
        await Assert.ThrowsAsync<NotFoundException>(() => save.Handle(new SaveClipCommand(user, Guid.NewGuid()), default));

        var saved = await new GetSavedClipsQueryHandler(context).Handle(new GetSavedClipsQuery(user), default);
        Assert.Equal(2, saved.Count);
        Assert.All(saved, c => Assert.True(c.IsSaved));

        var onlySaved = await new GetLearnerClipsQueryHandler(context).Handle(new GetLearnerClipsQuery(user, 20, SavedOnly: true), default);
        Assert.Equal(2, onlySaved.Count);
        Assert.DoesNotContain(onlySaved, c => c.Id == b.Id);

        var feed = await new GetLearnerClipsQueryHandler(context).Handle(new GetLearnerClipsQuery(user, 20), default);
        Assert.Equal(2, feed.Count); // yayındaki a ve b
        Assert.True(feed.Single(c => c.Id == a.Id).IsSaved);
        Assert.False(feed.Single(c => c.Id == b.Id).IsSaved);

        // Çıkarma da tekrar edilebilir; yeniden kaydedince aynı satır geri gelir.
        var unsave = new UnsaveClipCommandHandler(context);
        await unsave.Handle(new UnsaveClipCommand(user, a.Id), default);
        await unsave.Handle(new UnsaveClipCommand(user, a.Id), default);
        Assert.Single(await new GetSavedClipsQueryHandler(context).Handle(new GetSavedClipsQuery(user), default));
        await save.Handle(new SaveClipCommand(user, a.Id), default);
        Assert.Equal(2, await context.UserClips.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, (await new GetSavedClipsQueryHandler(context).Handle(new GetSavedClipsQuery(user), default)).Count);

        // Başka kullanıcının kayıtları ayrıdır.
        Assert.Empty(await new GetSavedClipsQueryHandler(context).Handle(new GetSavedClipsQuery(Guid.NewGuid()), default));
    }
}

public class BurnedInCaptionTests
{
    [Fact]
    public async Task Flag_is_set_on_every_clip_of_the_video_and_reaches_learners()
    {
        await using var context = new WordWiseDbContext(new DbContextOptionsBuilder<WordWiseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        VideoClip Make(string video) => new()
        {
            YoutubeId = video, Text = "Daddy is the best cook.", WordTimingsJson = "[]",
            QcStatus = ClipQcStatus.Passed, IsPublished = true,
        };
        context.VideoClips.AddRange(Make("aaaaaaaaaaa"), Make("aaaaaaaaaaa"), Make("bbbbbbbbbbb"));
        await context.SaveChangesAsync();

        var updated = await new WordWise.Application.Features.Clips.Commands.ReviewClip.SetBurnedInCaptionsCommandHandler(context)
            .Handle(new WordWise.Application.Features.Clips.Commands.ReviewClip.SetBurnedInCaptionsCommand("aaaaaaaaaaa", true), default);

        Assert.Equal(2, updated);
        var learner = await new GetLearnerClipsQueryHandler(context).Handle(new GetLearnerClipsQuery(Guid.NewGuid(), 20), default);
        Assert.Equal(2, learner.Count(c => c.HasBurnedInCaptions && c.YoutubeId == "aaaaaaaaaaa"));
        Assert.Single(learner, c => !c.HasBurnedInCaptions);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new WordWise.Application.Features.Clips.Commands.ReviewClip.SetBurnedInCaptionsCommandHandler(context)
                .Handle(new WordWise.Application.Features.Clips.Commands.ReviewClip.SetBurnedInCaptionsCommand("ccccccccccc", true), default));
    }
}
