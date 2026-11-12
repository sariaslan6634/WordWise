using WordWise.Application.Features.Words;
using Xunit;

namespace WordWise.Tests;

public class WordFormsTests
{
    [Theory]
    [InlineData("giving", "give")]
    [InlineData("running", "run")]
    [InlineData("cities", "city")]
    [InlineData("liked", "like")]
    [InlineData("stopped", "stop")]
    [InlineData("tries", "try")]
    [InlineData("dogs", "dog")]
    [InlineData("bigger", "big")]
    [InlineData("happiest", "happy")]
    [InlineData("You're", "you")]
    [InlineData("don't", "do")]
    [InlineData("can't", "can")]
    [InlineData("it's", "it")]
    public void Candidates_include_the_dictionary_form(string clicked, string expected)
    {
        Assert.Contains(expected, WordForms.Candidates(clicked));
    }

    [Fact]
    public void The_word_itself_comes_first_and_punctuation_is_stripped()
    {
        Assert.Equal("quick", WordForms.Candidates("“quick,”")[0]);
        Assert.Equal("talk", WordForms.Candidates("Talk.")[0]);
        Assert.Empty(WordForms.Candidates("..."));
        Assert.Equal(WordForms.Candidates("keep").Distinct().Count(), WordForms.Candidates("keep").Count);
    }
}
