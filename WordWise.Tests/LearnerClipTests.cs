using Microsoft.EntityFrameworkCore;
using WordWise.Application.Common.Exceptions;
using WordWise.Application.Features.Clips.Commands.ReportClip;
using WordWise.Application.Features.Clips.Queries.GetLearnerClips;
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
}
