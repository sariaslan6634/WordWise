using WordWise.WiktionaryImporter;
using Xunit;

namespace WordWise.Tests;

public class WiktionaryParserTests
{
    private const string Good =
        """{"word": "serendipity", "lang_code": "en", "lang": "English", "pos": "noun", "sounds": [{"tags": ["US"]}, {"ipa": "/ˌsɛɹ.ənˈdɪp.ɪ.ti/"}], "senses": [{"glosses": ["The occurrence of events by chance in a happy way."], "examples": [{"text": "It was pure serendipity that we met."}, {"text": "A long old quotation text here.", "type": "quotation"}], "synonyms": [{"word": "luck"}]}], "antonyms": [{"word": "misfortune"}]}""";

    [Fact]
    public void Parses_english_entry_with_definition_ipa_examples_and_related_words()
    {
        var w = WiktionaryParser.Parse(Good);

        Assert.NotNull(w);
        Assert.Equal("serendipity", w!.Text);
        Assert.Equal("noun", w.PartOfSpeech);
        Assert.Equal("/ˌsɛɹ.ənˈdɪp.ɪ.ti/", w.Ipa);
        Assert.Contains("by chance", w.Definition);
        Assert.Contains("pure serendipity", w.ExamplesJson);
        Assert.DoesNotContain("quotation", w.ExamplesJson);
        Assert.Contains("luck", w.SynonymsJson);
        Assert.Contains("misfortune", w.AntonymsJson);
    }

    [Theory]
    [InlineData("""{"word": "chat", "lang_code": "fr", "pos": "noun", "senses": [{"glosses": ["cat"]}]}""")]
    [InlineData("""{"word": "London", "lang_code": "en", "pos": "name", "senses": [{"glosses": ["A city."]}]}""")]
    [InlineData("""{"word": "NASA", "lang_code": "en", "pos": "noun", "senses": [{"glosses": ["An agency."]}]}""")]
    [InlineData("""{"word": "ice cream", "lang_code": "en", "pos": "noun", "senses": [{"glosses": ["A frozen dessert."]}]}""")]
    [InlineData("""{"word": "dogs", "lang_code": "en", "pos": "noun", "senses": [{"glosses": ["plural of dog"], "form_of": [{"word": "dog"}]}]}""")]
    [InlineData("""{"word": "thou", "lang_code": "en", "pos": "pron", "senses": [{"glosses": ["You."], "tags": ["archaic"]}]}""")]
    [InlineData("""{"word": "colour", "lang_code": "en", "pos": "noun", "senses": [{"glosses": ["Alternative spelling of color."]}]}""")]
    [InlineData("not json {\"lang_code\": \"en\"")]
    public void Skips_non_english_names_phrases_inflections_and_obsolete_entries(string line)
    {
        Assert.Null(WiktionaryParser.Parse(line));
    }

    [Fact]
    public void Uses_most_specific_gloss_and_skips_unusable_first_sense()
    {
        var line = """{"word": "run", "lang_code": "en", "pos": "verb", "senses": [{"glosses": ["Obsolete spelling."]}, {"glosses": ["To move", "To move quickly on foot."]}]}""";

        var w = WiktionaryParser.Parse(line);

        Assert.Equal("To move quickly on foot.", w!.Definition);
        Assert.Equal("verb", w.PartOfSpeech);
    }
}
