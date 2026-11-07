using System.Text.Json;
using System.Text.RegularExpressions;

namespace WordWise.WiktionaryImporter;

public sealed record ImportedWord(
    string Text,
    string Definition,
    string? Ipa,
    string PartOfSpeech,
    string? ExamplesJson,
    string? SynonymsJson,
    string? AntonymsJson);

/// <summary>
/// raw-wiktextract-data.jsonl satırlarını (her satır bir sözlük girdisi) WordWise "Words" kaydına çevirir.
/// Sadece İngilizce, tek kelimelik, küçük harfli (özel isim/kısaltma olmayan) ve çekim/eski/kaba olmayan girdileri alır.
/// </summary>
public static class WiktionaryParser
{
    private static readonly Regex WordRegex = new(@"^[a-z][a-z'\-]{0,38}[a-z]$", RegexOptions.Compiled);

    private static readonly Dictionary<string, string> PosNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["noun"] = "noun", ["verb"] = "verb", ["adj"] = "adjective", ["adv"] = "adverb",
        ["prep"] = "preposition", ["conj"] = "conjunction", ["pron"] = "pronoun",
        ["intj"] = "interjection", ["det"] = "determiner", ["num"] = "numeral",
    };

    private static readonly HashSet<string> BadTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "obsolete", "archaic", "dated", "rare", "nonstandard", "misspelling", "uncommon", "proscribed",
        "eye-dialect", "vulgar", "offensive", "derogatory", "slur", "ethnic-slur", "obscene",
    };

    private static readonly string[] BadGlossStarts =
    {
        "alternative form of", "alternative spelling of", "alternative letter-case form of", "obsolete", "archaic",
        "misspelling of", "plural of", "past tense of", "past participle of", "present participle of",
        "simple past", "third-person singular", "comparative form of", "superlative form of", "initialism of",
        "abbreviation of", "acronym of", "contraction of", "clipping of", "synonym of", "eye dialect",
        "inflection of", "gerund of", "nonstandard", "informal form of", "dated form of", "rare form of",
        "genitive", "feminine of", "masculine of", "diminutive of", "augmentative of",
    };

    /// <summary>Hızlı ön süzgeç: İngilizce olmayan satırları JSON'a çevirmeden eler (24 GB dosyada çok önemli).</summary>
    public static bool MightBeEnglish(string line) =>
        line.Contains("\"lang_code\": \"en\"", StringComparison.Ordinal) ||
        line.Contains("\"lang_code\":\"en\"", StringComparison.Ordinal);

    public static ImportedWord? Parse(string line)
    {
        if (!MightBeEnglish(line)) return null;

        JsonDocument doc;
        try { doc = JsonDocument.Parse(line); }
        catch (JsonException) { return null; }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            if (Str(root, "lang_code") != "en") return null;

            var text = Str(root, "word");
            if (text is null || !WordRegex.IsMatch(text)) return null;

            var rawPos = Str(root, "pos");
            if (rawPos is null || !PosNames.TryGetValue(rawPos, out var pos)) return null;

            if (!root.TryGetProperty("senses", out var senses) || senses.ValueKind != JsonValueKind.Array) return null;

            JsonElement? chosen = null;
            string? definition = null;
            foreach (var sense in senses.EnumerateArray())
            {
                if (sense.ValueKind != JsonValueKind.Object) continue;
                if (sense.TryGetProperty("form_of", out _) || sense.TryGetProperty("alt_of", out _)) continue;
                if (HasBadTag(sense)) continue;

                var gloss = LastGloss(sense);
                if (string.IsNullOrWhiteSpace(gloss) || gloss.EndsWith(':')) continue;
                if (BadGlossStarts.Any(b => gloss.StartsWith(b, StringComparison.OrdinalIgnoreCase))) continue;

                chosen = sense;
                definition = gloss.Length > 2000 ? gloss[..2000] : gloss;
                break;
            }
            if (chosen is null || definition is null) return null;

            var examples = new List<string>();
            if (chosen.Value.TryGetProperty("examples", out var ex) && ex.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in ex.EnumerateArray())
                {
                    if (e.ValueKind != JsonValueKind.Object) continue;
                    if (Str(e, "type") == "quotation") continue;
                    var t = Str(e, "text")?.Trim();
                    if (t is { Length: >= 8 and <= 200 } && !examples.Contains(t)) examples.Add(t);
                    if (examples.Count == 3) break;
                }
            }

            var synonyms = Related(chosen.Value, root, "synonyms");
            var antonyms = Related(chosen.Value, root, "antonyms");

            return new ImportedWord(
                text, definition, FirstIpa(root), pos,
                examples.Count > 0 ? JsonSerializer.Serialize(examples) : null,
                synonyms.Count > 0 ? JsonSerializer.Serialize(synonyms) : null,
                antonyms.Count > 0 ? JsonSerializer.Serialize(antonyms) : null);
        }
    }

    private static string? Str(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;

    private static bool HasBadTag(JsonElement sense)
    {
        if (!sense.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Array) return false;
        return tags.EnumerateArray().Any(t => t.ValueKind == JsonValueKind.String && BadTags.Contains(t.GetString() ?? ""));
    }

    private static string? LastGloss(JsonElement sense)
    {
        if (!sense.TryGetProperty("glosses", out var glosses) || glosses.ValueKind != JsonValueKind.Array) return null;
        string? last = null;
        foreach (var g in glosses.EnumerateArray())
            if (g.ValueKind == JsonValueKind.String) last = g.GetString();
        return last?.Trim();
    }

    private static string? FirstIpa(JsonElement root)
    {
        if (!root.TryGetProperty("sounds", out var sounds) || sounds.ValueKind != JsonValueKind.Array) return null;
        foreach (var s in sounds.EnumerateArray())
        {
            var ipa = Str(s, "ipa")?.Trim();
            if (!string.IsNullOrEmpty(ipa)) return ipa.Length > 100 ? ipa[..100] : ipa;
        }
        return null;
    }

    private static List<string> Related(JsonElement sense, JsonElement root, string field)
    {
        var result = new List<string>();
        foreach (var source in new[] { sense, root })
        {
            if (!source.TryGetProperty(field, out var arr) || arr.ValueKind != JsonValueKind.Array) continue;
            foreach (var item in arr.EnumerateArray())
            {
                var w = Str(item, "word")?.Trim();
                if (w is not null && WordRegex.IsMatch(w) && !result.Contains(w)) result.Add(w);
                if (result.Count == 5) return result;
            }
        }
        return result;
    }
}
