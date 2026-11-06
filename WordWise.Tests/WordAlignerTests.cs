using WordWise.Application.Features.Clips.Models;
using WordWise.Application.Features.Clips.Services;
using Xunit;

namespace WordWise.Tests;

public class WordAlignerTests
{
    private static List<CaptionWord> Words(params (string Text, double Start, double End)[] items) =>
        items.Select(i => new CaptionWord(i.Text, i.Start, i.End)).ToList();

    [Fact]
    public void Aligns_manual_caption_words_to_real_asr_times()
    {
        var clip = ClipSelector.EstimateWordTimes(new[] { "Come", "on,", "be", "careful." }, 10, 17);
        var asr = Words(("come", 10.2, 10.45), ("on", 10.45, 10.7), ("be", 10.8, 10.95), ("careful", 10.95, 11.5));

        var aligned = WordAligner.Align(clip, asr, 10, 17);

        Assert.NotNull(aligned);
        Assert.Equal(new[] { "Come", "on,", "be", "careful." }, aligned!.Select(w => w.Text).ToArray());
        Assert.Equal(10.2, aligned[0].Start, 2);
        Assert.Equal(10.95, aligned[3].Start, 2);
        Assert.Equal(11.5, aligned[3].End, 2);
    }

    [Fact]
    public void Interpolates_unmatched_words_between_matched_neighbours_and_stays_ordered()
    {
        // Elle girilmiş "going to", otomatik kayıtta "gonna" olarak geçiyor
        var clip = ClipSelector.EstimateWordTimes(new[] { "I", "am", "going", "to", "leave", "now" }, 50, 54);
        var asr = Words(("i", 50.1, 50.3), ("am", 50.3, 50.5), ("gonna", 50.5, 50.9), ("leave", 51.2, 51.6), ("now", 51.6, 52.0));

        var aligned = WordAligner.Align(clip, asr, 50, 54);

        Assert.NotNull(aligned);
        Assert.Equal(6, aligned!.Count);
        Assert.Equal(51.2, aligned[4].Start, 2);          // "leave" gerçek zamanında
        Assert.InRange(aligned[2].Start, 50.5, 51.2);     // "going" iki komşu arasında
        Assert.InRange(aligned[3].Start, 50.5, 51.2);
        for (var i = 1; i < aligned.Count; i++)
            Assert.True(aligned[i].Start >= aligned[i - 1].Start - 0.001);
    }

    [Fact]
    public void Returns_null_when_too_few_words_match()
    {
        var clip = ClipSelector.EstimateWordTimes(new[] { "Completely", "different", "sentence", "here" }, 10, 13);
        var asr = Words(("something", 10.1, 10.5), ("else", 10.5, 10.9), ("entirely", 10.9, 11.3), ("here", 11.3, 11.6));

        Assert.Null(WordAligner.Align(clip, asr, 10, 13));
    }

    [Fact]
    public void Selector_uses_asr_times_and_marks_timings_as_real()
    {
        var lines = new List<CaptionLine> { new(240.24, 7, "Come on, be careful.") };
        var asr = Words(("come", 240.4, 240.65), ("on", 240.65, 240.9), ("be", 241.0, 241.15), ("careful", 241.15, 241.8));

        var clip = Assert.Single(ClipSelector.Select(lines, videoDurationSec: 300, asrWords: asr));

        Assert.True(clip.TimingsReal);
        Assert.Equal(240.4, clip.Words[0].Start, 2);
        Assert.Equal(241.8, clip.Words[^1].End, 2);
        Assert.InRange(clip.End, 241.8, 242.3); // klip konuşmanın bittiği yerde biter
    }
}
