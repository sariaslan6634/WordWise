using System.Net;
using System.Text.RegularExpressions;
using WordWise.Application.Features.Clips.Models;

namespace WordWise.Application.Features.Clips.Services
{
    /// <summary>
    /// Kural tabanlı klip seçici ajan: altyazı satırlarını cümlelere böler, uygun olanları puanlar
    /// ve videonun farklı yerlerinden birbirinden uzak en iyi cümleleri seçer.
    /// </summary>
    public static class ClipSelector
    {
        private static readonly Regex NoiseRegex = new(@"\[[^\]]*\]|\([^)]*\)|[♪♫]|>>", RegexOptions.Compiled);
        private static readonly Regex SpaceRegex = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex MidSentenceBreak = new(@"[.!?][""')\]]?\s+\p{Lu}", RegexOptions.Compiled);
        private static readonly Regex EndsSentenceRegex = new(@"[.!?][""')\]]?$", RegexOptions.Compiled);
        private static readonly Regex WordRegex = new(@"\p{L}[\p{L}\p{N}'’\-]*|\p{N}+", RegexOptions.Compiled);

        public static string Clean(string raw)
        {
            var text = WebUtility.HtmlDecode(raw ?? string.Empty);
            text = NoiseRegex.Replace(text, " ");
            return SpaceRegex.Replace(text, " ").Trim();
        }

        public static int WordCount(string text) => WordRegex.Matches(text).Count;

        public static List<ClipCandidate> Select(
            IReadOnlyList<CaptionLine> lines,
            ClipSelectorOptions? options = null,
            double? videoDurationSec = null)
        {
            options ??= new ClipSelectorOptions();
            var ordered = lines.OrderBy(l => l.Start).ToList();
            if (ordered.Count == 0) return new List<ClipCandidate>();

            var cleaned = ordered.Select(l => Clean(l.Text)).ToList();
            var punctuatedLines = cleaned.Count(c => c.Length > 0 && EndsSentenceRegex.IsMatch(c));
            var transcriptIsPunctuated = punctuatedLines >= Math.Max(1, ordered.Count / 5);

            // 1) Satırları cümle gruplarına böl
            var groups = new List<List<int>>();
            var buffer = new List<int>();
            void Flush()
            {
                if (buffer.Count > 0) groups.Add(buffer);
                buffer = new List<int>();
            }

            for (var i = 0; i < ordered.Count; i++)
            {
                if (cleaned[i].Length == 0) { Flush(); continue; }

                if (buffer.Count > 0 && ordered[i].Start - ordered[buffer[^1]].End > 1.5)
                    Flush();

                buffer.Add(i);
                var bufferWords = WordCount(string.Join(" ", buffer.Select(b => cleaned[b])));

                if (EndsSentenceRegex.IsMatch(cleaned[i]))
                {
                    Flush();
                }
                else if (!transcriptIsPunctuated)
                {
                    var hasNext = i + 1 < ordered.Count;
                    var gapToNext = hasNext ? ordered[i + 1].Start - ordered[i].End : double.MaxValue;
                    if (!hasNext || gapToNext >= 0.6 || bufferWords >= options.MaxWords)
                        Flush();
                }
                else if (bufferWords > options.MaxWords * 2)
                {
                    buffer = new List<int>(); // kontrolsüz uzayan, noktasız parça
                }
            }
            Flush();

            // 2) Gruplardan aday cümleler üret
            var scored = new List<(ClipCandidate Clip, double Score)>();
            var seen = new HashSet<string>();
            foreach (var group in groups)
            {
                var text = string.Join(" ", group.Select(g => cleaned[g]));
                var wordCount = WordCount(text);
                var start = ordered[group[0]].Start;
                var end = ordered[group[^1]].End;
                var duration = end - start;

                if (wordCount < options.MinWords || wordCount > options.MaxWords) continue;
                if (duration < options.MinDurationSec || duration > options.MaxDurationSec) continue;
                if (transcriptIsPunctuated && MidSentenceBreak.IsMatch(text)) continue; // tek cümle olmalı
                var letters = text.Count(char.IsLetter);
                if (letters < text.Count(c => !char.IsWhiteSpace(c)) * 0.7) continue;
                if (!seen.Add(Normalize(text))) continue;

                var words = BuildWords(group.Select(g => ordered[g]).ToList(), text, start, end);
                var rate = wordCount / duration;
                var score = -Math.Abs(wordCount - 8)
                            - Math.Abs(rate - 2.8) * 2
                            + (EndsSentenceRegex.IsMatch(text) ? 2 : 0)
                            + (char.IsUpper(text[0]) ? 1 : 0);

                var padStart = Math.Max(0, start - options.PaddingBeforeSec);
                var padEnd = end + options.PaddingAfterSec;
                if (videoDurationSec.HasValue) padEnd = Math.Min(padEnd, videoDurationSec.Value);

                scored.Add((new ClipCandidate(Math.Round(padStart, 2), Math.Round(padEnd, 2), text, words), score));
            }

            // 3) En iyileri seç, birbirine yakın başlayanları ele
            var picked = new List<ClipCandidate>();
            foreach (var (clip, _) in scored.OrderByDescending(s => s.Score).ThenBy(s => s.Clip.Start))
            {
                if (picked.Count >= options.MaxClips) break;
                if (picked.Any(p => Math.Abs(p.Start - clip.Start) < options.MinGapSec)) continue;
                picked.Add(clip);
            }

            return picked.OrderBy(p => p.Start).ToList();
        }

        /// <summary>Kelime zamanı altyazıda varsa onu, yoksa harf sayısına orantılı tahmini kullanır.</summary>
        private static List<CaptionWord> BuildWords(List<CaptionLine> group, string text, double start, double end)
        {
            var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (group.All(l => l.Words is { Count: > 0 }))
            {
                var real = group.SelectMany(l => l.Words!).ToList();
                if (real.Count == tokens.Length)
                    return real.Select((w, i) => new CaptionWord(tokens[i], w.Start, Math.Max(w.End, w.Start))).ToList();
            }

            return EstimateWordTimes(tokens, start, end);
        }

        public static List<CaptionWord> EstimateWordTimes(IReadOnlyList<string> tokens, double start, double end)
        {
            var result = new List<CaptionWord>();
            if (tokens.Count == 0 || end <= start) return result;

            var weights = tokens.Select(t => Math.Max(t.Count(char.IsLetterOrDigit), 1)).ToList();
            var total = weights.Sum();
            var cursor = start;
            for (var i = 0; i < tokens.Count; i++)
            {
                var duration = (end - start) * weights[i] / total;
                result.Add(new CaptionWord(tokens[i], Math.Round(cursor, 2), Math.Round(cursor + duration, 2)));
                cursor += duration;
            }
            return result;
        }

        public static string Normalize(string text) =>
            new string(text.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    }
}
