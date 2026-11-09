using System.Text;

namespace WordWise.WiktionaryImporter;

/// <summary>CEFR seviyesi dosyalarını okur (A1=1 ... C2=6, Word.CefrLevel ile aynı sayılar).</summary>
public static class CefrLevels
{
    /// <summary>"A1", "b2", "A1.2", "C1/C2" gibi değerlerden ilk seviyeyi çıkarır; tanınmazsa null.</summary>
    public static int? ParseLevel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < 2) return null;
        var v = value.Trim().ToUpperInvariant();
        if (v[0] is < 'A' or > 'C' || v[1] is < '1' or > '2') return null;
        return (v[0] - 'A') * 2 + (v[1] - '0');
    }

    /// <summary>Sıklık sırasına göre yaklaşık seviye: gerçek CEFR değil, kaba bir tahmindir.</summary>
    public static int? FromRank(int rank) => rank switch
    {
        <= 0 => null,
        <= 1000 => 1,
        <= 2500 => 2,
        <= 5000 => 3,
        <= 10000 => 4,
        <= 20000 => 5,
        <= 40000 => 6,
        _ => null,
    };

    private static readonly string[] WordColumns = { "headword", "word", "lemma", "base_form", "baseform" };
    private static readonly string[] LevelColumns = { "cefr", "level", "cefr level", "cefr-j level", "cefrlevel", "cefr_level" };

    /// <summary>
    /// CSV (başlık satırlı) içinden kelime → en kolay seviye. Aynı kelime birden fazla satırdaysa (farklı türler) en düşük seviye alınır.
    /// Başlık adları esnektir: headword/word/lemma ve CEFR/level.
    /// </summary>
    public static Dictionary<string, int> ParseCefrCsv(IEnumerable<string> lines)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int wordCol = -1, levelCol = -1;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cells = SplitCsv(line);
            if (wordCol < 0)
            {
                var header = cells.Select(c => c.Trim().TrimStart('﻿').ToLowerInvariant()).ToList();
                wordCol = header.FindIndex(h => WordColumns.Contains(h));
                levelCol = header.FindIndex(h => LevelColumns.Contains(h));
                if (wordCol < 0 || levelCol < 0) throw new InvalidDataException(
                    $"CSV başlığında kelime ({string.Join("/", WordColumns)}) ve seviye ({string.Join("/", LevelColumns)}) sütunları bulunamadı. Bulunanlar: {string.Join(", ", header)}");
                continue;
            }
            if (cells.Count <= Math.Max(wordCol, levelCol)) continue;
            var level = ParseLevel(cells[levelCol]);
            if (level is null) continue;
            foreach (var word in cells[wordCol].Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var key = word.ToLowerInvariant();
                if (!result.TryGetValue(key, out var existing) || level < existing) result[key] = level.Value;
            }
        }
        return result;
    }

    /// <summary>Sıklığa göre sıralı kelime listesi ("word" veya "word count" satırları) → yaklaşık seviye.</summary>
    public static Dictionary<string, int> ParseFrequencyList(IEnumerable<string> lines)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var rank = 0;
        foreach (var line in lines)
        {
            var word = line.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (string.IsNullOrEmpty(word)) continue;
            rank++;
            var level = FromRank(rank);
            if (level is null) break;
            result.TryAdd(word.ToLowerInvariant(), level.Value);
        }
        return result;
    }

    internal static List<string> SplitCsv(string line)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                else if (ch == '"') quoted = false;
                else current.Append(ch);
            }
            else if (ch == '"') quoted = true;
            else if (ch == ',') { cells.Add(current.ToString()); current.Clear(); }
            else current.Append(ch);
        }
        cells.Add(current.ToString());
        return cells;
    }
}
