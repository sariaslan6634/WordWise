using System.Text.RegularExpressions;

namespace WordWise.Application.Features.WordAudio
{
    /// <summary>Bir ses dosyasının hangi kelimeye ve hangi aksana ait olduğu.</summary>
    public sealed record WordAudioEntry(string Word, string Accent, string Path, string ContentType, int Rank);

    /// <summary>
    /// Wiktionary ses dosyası adlarını (ör. "En-GB-augment-noun.ogg.mp3", "En-gb-Jam_%28fruit_spread%29.ogg")
    /// kelime + aksan ("us" veya "gb") olarak çözer ve her (kelime, aksan) için en iyi dosyayı seçer.
    /// </summary>
    public sealed class WordAudioIndex
    {
        public const string Us = "us";
        public const string Gb = "gb";

        private static readonly Regex NameRegex = new(@"^en-(us|gb|uk)-(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex QualifierRegex = new(@"\s*\([^)]*\)\s*$", RegexOptions.Compiled);
        private static readonly Regex PosSuffixRegex = new(@"-(noun|verb|adjective|adj|adverb|adv|preposition|pronoun)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly string[] Extensions = { ".mp3", ".ogg", ".oga" };

        private readonly Dictionary<(string Word, string Accent), WordAudioEntry> _entries = new();

        public int Count => _entries.Count;

        public static WordAudioIndex Build(IEnumerable<string> filePaths)
        {
            var index = new WordAudioIndex();
            // Sıralı gezmek, aynı derecedeki dosyalarda hep aynı dosyanın seçilmesini sağlar.
            foreach (var path in filePaths.OrderBy(p => p, StringComparer.Ordinal))
            {
                var entry = Parse(path);
                if (entry is null) continue;
                var key = (entry.Word, entry.Accent);
                if (!index._entries.TryGetValue(key, out var existing) || entry.Rank < existing.Rank)
                    index._entries[key] = entry;
            }
            return index;
        }

        /// <summary>Dosya adından kayıt üretir; İngilizce us/gb/uk ses dosyası değilse null.</summary>
        public static WordAudioEntry? Parse(string path)
        {
            var name = System.IO.Path.GetFileName(path).ToLowerInvariant();

            // "x.ogg.mp3" gibi çift uzantıda oynatılacak olan son uzantıdır.
            var ext = Extensions.FirstOrDefault(e => name.EndsWith(e, StringComparison.Ordinal));
            if (ext is null) return null;
            name = name[..^ext.Length];
            foreach (var inner in Extensions)
                if (name.EndsWith(inner, StringComparison.Ordinal)) { name = name[..^inner.Length]; break; }

            try { name = Uri.UnescapeDataString(name); } catch (UriFormatException) { return null; }

            var match = NameRegex.Match(name);
            if (!match.Success) return null;
            var accent = match.Groups[1].Value == "us" ? Us : Gb;
            var rest = match.Groups[2].Value.Replace('_', ' ').Trim();

            var variant = 0;
            if (QualifierRegex.IsMatch(rest)) { rest = QualifierRegex.Replace(rest, ""); variant = 2; }
            else if (PosSuffixRegex.IsMatch(rest)) { rest = PosSuffixRegex.Replace(rest, ""); variant = 1; }
            rest = rest.Trim();
            if (rest.Length == 0) return null;

            var formatRank = ext == ".mp3" ? 0 : ext == ".ogg" ? 1 : 2;
            var contentType = ext == ".mp3" ? "audio/mpeg" : "audio/ogg";
            return new WordAudioEntry(rest, accent, path, contentType, variant * 10 + formatRank);
        }

        public WordAudioEntry? Find(string word, string accent) =>
            _entries.TryGetValue((word.Trim().ToLowerInvariant(), accent), out var entry) ? entry : null;

        /// <summary>Kelime için mevcut aksanlar, her zaman ["us", "gb"] sırasıyla.</summary>
        public List<string> AccentsFor(string word)
        {
            var key = word.Trim().ToLowerInvariant();
            var result = new List<string>();
            if (_entries.ContainsKey((key, Us))) result.Add(Us);
            if (_entries.ContainsKey((key, Gb))) result.Add(Gb);
            return result;
        }
    }
}
