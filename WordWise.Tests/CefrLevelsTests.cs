using WordWise.WiktionaryImporter;
using Xunit;

namespace WordWise.Tests;

public class CefrLevelsTests
{
    [Theory]
    [InlineData("A1", 1)]
    [InlineData("a2", 2)]
    [InlineData("B1", 3)]
    [InlineData("B2", 4)]
    [InlineData("C1", 5)]
    [InlineData("C2", 6)]
    [InlineData("A1.2", 1)]
    [InlineData("C1/C2", 5)]
    public void Parses_cefr_labels_to_word_level_numbers(string label, int expected)
    {
        Assert.Equal(expected, CefrLevels.ParseLevel(label));
    }

    [Theory]
    [InlineData("")]
    [InlineData("D1")]
    [InlineData("A3")]
    [InlineData(null)]
    public void Ignores_unknown_labels(string? label)
    {
        Assert.Null(CefrLevels.ParseLevel(label));
    }

    [Fact]
    public void Cefr_csv_uses_flexible_headers_quotes_and_keeps_the_easiest_level_per_word()
    {
        var csv = new[]
        {
            "﻿headword,pos,CEFR,notes",
            "book,noun,A1,\"a, thing\"",
            "book,verb,B1,",
            "run,verb,A1.2,",
            "a.m./A.M.,adverb,A1,",
            "weird,adj,Z9,",
        };

        var result = CefrLevels.ParseCefrCsv(csv);

        Assert.Equal(1, result["book"]);
        Assert.Equal(1, result["run"]);
        Assert.Equal(1, result["a.m."]);
        Assert.False(result.ContainsKey("weird"));
    }

    [Fact]
    public void Cefr_csv_without_known_columns_fails_with_a_clear_message()
    {
        var error = Assert.Throws<InvalidDataException>(() => CefrLevels.ParseCefrCsv(new[] { "foo,bar", "x,y" }));
        Assert.Contains("sütunları bulunamadı", error.Message);
    }

    [Fact]
    public void Frequency_list_maps_rank_to_approximate_level_and_stops_after_the_last_band()
    {
        var words = Enumerable.Range(1, 45000).Select(i => $"w{i} {50000 - i}");

        var result = CefrLevels.ParseFrequencyList(words);

        Assert.Equal(1, result["w1"]);
        Assert.Equal(1, result["w1000"]);
        Assert.Equal(2, result["w1001"]);
        Assert.Equal(4, result["w10000"]);
        Assert.Equal(6, result["w40000"]);
        Assert.False(result.ContainsKey("w40001"));
    }
}
