using WordWise.Application.Features.Clips.Models;
using WordWise.Application.Features.Clips.Services;
using WordWise.Infrastructure.Services;
using Xunit;

namespace WordWise.Tests;

public class ClipAgentTests
{
    private static CaptionLine Line(double start, double dur, string text) => new(start, dur, text);

    private static VideoDetails GoodDetails() => new(
        "abc", "English pronunciation lesson", "A clean lesson", "Channel",
        new List<string> { "english" }, 180, "hd", true, "public", false);

    private static List<CaptionLine> SampleLines() => new()
    {
        Line(2, 2.5, "[Music]"),
        Line(10, 3, "Daddy is the best cook in the world."),
        Line(14, 2, "Thanks!"),
        Line(40, 3.5, "I really think that we should leave now."),
        Line(45, 3, "She told me she would come tomorrow."),
        Line(75, 3, "It was the beginning of a long journey."),
        Line(110, 3, "Hello. How are you? I am fine today."),
        Line(150, 3, "We need to talk about the plan again."),
    };

    [Fact]
    public void Selector_picks_single_sentences_spaced_apart_and_skips_noise_and_multi_sentence_lines()
    {
        var clips = ClipSelector.Select(SampleLines(), videoDurationSec: 180);

        Assert.NotEmpty(clips);
        Assert.All(clips, c => Assert.DoesNotContain("[Music]", c.Text));
        Assert.DoesNotContain(clips, c => c.Text.Contains("Hello. How"));
        Assert.DoesNotContain(clips, c => c.Text == "Thanks!"); // 1 kelime < 3

        for (var i = 1; i < clips.Count; i++)
            Assert.True(clips[i].Start - clips[i - 1].Start >= 20 - 0.5);
    }

    [Fact]
    public void Selector_merges_two_speakers_into_one_dialogue_clip_instead_of_failing()
    {
        // İki kişi üst üste konuşuyor: altyazı satırları zamanda çakışıyor
        var lines = new List<CaptionLine>
        {
            Line(100, 3, "Come on, be careful."),
            Line(101.5, 3, "I am fine, thank you."),
        };
        var track = new CaptionTrack("en", false, lines);

        var clip = Assert.Single(ClipSelector.Select(lines, videoDurationSec: 180));
        Assert.Contains("careful.", clip.Text);
        Assert.Contains("thank you.", clip.Text);
        Assert.Equal(9, clip.Words.Count);

        var result = ClipQualityChecker.Check(clip, GoodDetails(), track);
        Assert.True(result.Passed, string.Join(" | ", result.Reasons));
    }

    [Fact]
    public void Selector_respects_max_clip_count()
    {
        var options = new ClipSelectorOptions { MaxClips = 2 };
        var clips = ClipSelector.Select(SampleLines(), options, 180);
        Assert.True(clips.Count <= 2);
    }

    [Fact]
    public void Selector_word_times_are_ordered_and_cover_every_word()
    {
        var clip = ClipSelector.Select(SampleLines(), videoDurationSec: 180).First();
        var tokens = clip.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(tokens.Length, clip.Words.Count);
        for (var i = 1; i < clip.Words.Count; i++)
            Assert.True(clip.Words[i].Start >= clip.Words[i - 1].Start);
    }

    [Fact]
    public void Selector_caps_speech_duration_when_caption_stays_long_on_screen()
    {
        // Altyazı 7 sn ekranda kalıyor ama 4 kelimelik cümle ~2.4 sn sürer
        var lines = new List<CaptionLine> { Line(240.24, 7, "Come on, be careful.") };
        var track = new CaptionTrack("en", false, lines);

        var clip = Assert.Single(ClipSelector.Select(lines, videoDurationSec: 300));
        var span = clip.Words[^1].End - clip.Words[0].Start;
        Assert.True(span < 3.0, $"konuşma süresi {span}");
        Assert.Equal(7, clip.CaptionSeconds);

        var longVideo = GoodDetails() with { DurationSeconds = 300 }; // klip 240. sn civarında
        var result = ClipQualityChecker.Check(clip, longVideo, track);
        Assert.True(result.Passed, string.Join(" | ", result.Reasons));
        Assert.Contains(result.Warnings, w => w.Contains("ekranda"));
    }

    [Fact]
    public void Quality_checker_approves_clean_clip()
    {
        var lines = SampleLines();
        var track = new CaptionTrack("en", false, lines);
        var clip = ClipSelector.Select(lines, videoDurationSec: 180).First();

        var result = ClipQualityChecker.Check(clip, GoodDetails(), track);

        Assert.True(result.Passed, string.Join(" | ", result.Reasons));
        Assert.Empty(result.Reasons);
    }

    [Fact]
    public void Quality_checker_rejects_profanity_with_reason()
    {
        var lines = new List<CaptionLine> { Line(10, 3, "This is a damn good shit show for everyone.") };
        var track = new CaptionTrack("en", false, lines);
        var clip = new ClipCandidate(10, 13, lines[0].Text, ClipSelector.EstimateWordTimes(lines[0].Text.Split(' '), 10, 13));

        var result = ClipQualityChecker.Check(clip, GoodDetails(), track);

        Assert.False(result.Passed);
        Assert.Contains(result.Reasons, r => r.Contains("küfür"));
    }

    [Fact]
    public void Quality_checker_rejects_low_quality_age_restricted_and_adult_metadata()
    {
        var lines = SampleLines();
        var track = new CaptionTrack("en", false, lines);
        var clip = ClipSelector.Select(lines, videoDurationSec: 180).First();
        var bad = GoodDetails() with { Definition = "sd", AgeRestricted = true, Title = "NSFW xxx video" };

        var result = ClipQualityChecker.Check(clip, bad, track);

        Assert.False(result.Passed);
        Assert.Contains(result.Reasons, r => r.Contains("HD"));
        Assert.Contains(result.Reasons, r => r.Contains("yaş sınırlı"));
        Assert.Contains(result.Reasons, r => r.Contains("yetişkin"));
    }

    [Fact]
    public void Quality_checker_rejects_unrealistic_speech_rate()
    {
        var lines = new List<CaptionLine> { Line(10, 0.5, "This sentence has far too many words inside.") };
        var track = new CaptionTrack("en", false, lines);
        var text = lines[0].Text;
        var clip = new ClipCandidate(10, 10.5, text, ClipSelector.EstimateWordTimes(text.Split(' '), 10, 10.5));

        var result = ClipQualityChecker.Check(clip, GoodDetails(), track);

        Assert.False(result.Passed);
        Assert.Contains(result.Reasons, r => r.Contains("Konuşma hızı"));
    }

    [Fact]
    public void Content_filters_do_not_flag_innocent_words()
    {
        Assert.Empty(ContentFilters.FindProfanity("The class passed the assassin exam in Scunthorpe."));
        Assert.Empty(ContentFilters.FindAdultTerms("Photography is a lesson on grammar."));
        Assert.NotEmpty(ContentFilters.FindProfanity("what the f**k"));
    }

    [Fact]
    public void Parser_reads_json3_lines_and_word_offsets()
    {
        const string json = """
        {"events":[
          {"tStartMs":1000,"dDurationMs":3000,"segs":[{"utf8":"Hello"},{"utf8":" big","tOffsetMs":600},{"utf8":" world","tOffsetMs":1500}]},
          {"tStartMs":5000,"dDurationMs":2000,"segs":[{"utf8":"Second line &amp; more\n"}]},
          {"tStartMs":8000,"dDurationMs":1000}
        ]}
        """;

        var lines = CaptionParser.ParseJson3(json);

        Assert.Equal(2, lines.Count);
        Assert.Equal("Hello big world", lines[0].Text);
        Assert.NotNull(lines[0].Words);
        Assert.Equal(3, lines[0].Words!.Count);
        Assert.Equal(1.6, lines[0].Words![1].Start, 3);
        Assert.Equal("Second line & more", lines[1].Text);
        Assert.Null(lines[1].Words);
    }

    [Fact]
    public void Parser_extracts_english_tracks_with_manual_captions_first()
    {
        const string html = """
        ...ytInitialPlayerResponse = {"captions":{"playerCaptionsTracklistRenderer":{"captionTracks":[
        {"baseUrl":"https://www.youtube.com/api/timedtext?v=x&lang=en&kind=asr","languageCode":"en","kind":"asr"},
        {"baseUrl":"https://www.youtube.com/api/timedtext?v=x&lang=de","languageCode":"de"},
        {"baseUrl":"https://www.youtube.com/api/timedtext?v=x&lang=en","languageCode":"en"}
        ],"audioTracks":[]}}};
        """;

        var tracks = CaptionParser.ExtractEnglishTracks(html);

        Assert.Equal(2, tracks.Count);
        Assert.False(tracks[0].IsAutoGenerated);
        Assert.True(tracks[1].IsAutoGenerated);
        Assert.Contains("&lang=en", tracks[0].BaseUrl);
    }
}

public class YoutubeIdTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=rxUm-2x-2dM", "rxUm-2x-2dM")]
    [InlineData("https://www.youtube.com/watch?list=PL123&v=rxUm-2x-2dM&t=30s", "rxUm-2x-2dM")]
    [InlineData("https://youtu.be/rxUm-2x-2dM?si=abc", "rxUm-2x-2dM")]
    [InlineData("https://www.youtube.com/shorts/rxUm-2x-2dM", "rxUm-2x-2dM")]
    [InlineData("rxUm-2x-2dM", "rxUm-2x-2dM")]
    public void Extracts_video_id_from_urls_and_plain_ids(string input, string expected)
    {
        Assert.Equal(expected, WordWise.Application.Features.Clips.Services.ClipGenerator.ExtractYoutubeId(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a link")]
    [InlineData("https://example.com/page")]
    public void Returns_null_for_invalid_input(string input)
    {
        Assert.Null(WordWise.Application.Features.Clips.Services.ClipGenerator.ExtractYoutubeId(input));
    }
}
