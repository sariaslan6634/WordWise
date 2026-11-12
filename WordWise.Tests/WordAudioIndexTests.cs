using WordWise.Application.Features.WordAudio;
using Xunit;

namespace WordWise.Tests;

public class WordAudioIndexTests
{
    [Theory]
    [InlineData("En-GB-augment-noun.ogg", "augment", "gb")]
    [InlineData("En-GB-augment-noun.ogg.mp3", "augment", "gb")]
    [InlineData("En-us-beautiful.ogg.mp3", "beautiful", "us")]
    [InlineData("En-uk-beautiful.oga", "beautiful", "gb")]
    [InlineData("En-gb-Jam_%28fruit_spread%29.ogg", "jam", "gb")]
    [InlineData("En-us-ice_cream.mp3", "ice cream", "us")]
    public void Parses_word_and_accent_from_file_name(string file, string word, string accent)
    {
        var entry = WordAudioIndex.Parse(@"D:\audio\" + file);

        Assert.NotNull(entry);
        Assert.Equal(word, entry!.Word);
        Assert.Equal(accent, entry.Accent);
    }

    [Theory]
    [InlineData("En-au-beautiful.ogg")]
    [InlineData("Fr-beautiful.ogg")]
    [InlineData("En-us-beautiful.txt")]
    [InlineData("En-us-.mp3")]
    public void Ignores_other_accents_languages_and_formats(string file)
    {
        Assert.Null(WordAudioIndex.Parse(file));
    }

    [Fact]
    public void Prefers_mp3_over_ogg_and_plain_names_over_qualified_ones()
    {
        var index = WordAudioIndex.Build(new[]
        {
            "En-us-jam.ogg", "En-us-jam.ogg.mp3", "En-us-jam-noun.ogg.mp3",
            "En-gb-Jam_%28fruit_spread%29.ogg.mp3",
        });

        Assert.EndsWith("En-us-jam.ogg.mp3", index.Find("Jam", "us")!.Path);
        Assert.Equal("audio/mpeg", index.Find("jam", "us")!.ContentType);
        Assert.NotNull(index.Find("jam", "gb"));
        Assert.Equal(new[] { "us", "gb" }, index.AccentsFor("jam"));
    }

    [Fact]
    public void Reports_only_accents_that_exist()
    {
        var index = WordAudioIndex.Build(new[] { "En-GB-augment-noun.ogg", "En-GB-augment-noun.ogg.mp3" });

        Assert.Equal(new[] { "gb" }, index.AccentsFor("augment"));
        Assert.Empty(index.AccentsFor("beautiful"));
        Assert.Null(index.Find("augment", "us"));
    }

    [Fact]
    public void Entries_added_later_are_found_and_do_not_replace_a_better_file()
    {
        var index = WordAudioIndex.Build(new[] { @"D:\audio\En-us-beautiful.ogg.mp3" });
        Assert.Empty(index.AccentsFor("did"));

        index.Add(WordAudioIndex.Parse(@"D:\audio\fetched\En-us-did.ogg")!);
        Assert.Equal(new[] { "us" }, index.AccentsFor("did"));

        index.Add(WordAudioIndex.Parse(@"D:\audio\fetched\En-us-beautiful.ogg")!);
        Assert.EndsWith(".mp3", index.Find("beautiful", "us")!.Path); // mp3 daha iyi sırada kalır
    }
}
