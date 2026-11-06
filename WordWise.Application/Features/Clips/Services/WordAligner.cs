using WordWise.Application.Features.Clips.Models;

namespace WordWise.Application.Features.Clips.Services
{
    /// <summary>
    /// Elle girilmiş altyazı cümlesinin kelimelerini, YouTube'un otomatik altyazı kaydındaki
    /// gerçek kelime zamanlarıyla eşleştirir (ses indirmeden doğru senkron).
    /// </summary>
    public static class WordAligner
    {
        private const double ContextSec = 2.0;
        private const double MinMatchRatio = 0.6;
        private const double FallbackWordSec = 0.35;

        private static string Norm(string text) =>
            new string(text.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        /// <summary>Eşleşme yetersizse null döner (çağıran tahmini zamanları kullanır).</summary>
        public static List<CaptionWord>? Align(
            IReadOnlyList<CaptionWord> clipWords,
            IReadOnlyList<CaptionWord> asrWords,
            double windowStart,
            double windowEnd)
        {
            var n = clipWords.Count;
            if (n == 0) return null;

            var asr = asrWords
                .Where(w => w.Start >= windowStart - ContextSec && w.Start <= windowEnd + ContextSec)
                .OrderBy(w => w.Start)
                .ToList();
            var m = asr.Count;
            if (m == 0) return null;

            var a = clipWords.Select(w => Norm(w.Text)).ToArray();
            var b = asr.Select(w => Norm(w.Text)).ToArray();

            // En uzun ortak alt dizi (LCS)
            var dp = new int[n + 1, m + 1];
            for (var i = n - 1; i >= 0; i--)
                for (var j = m - 1; j >= 0; j--)
                    dp[i, j] = a[i].Length > 0 && a[i] == b[j]
                        ? dp[i + 1, j + 1] + 1
                        : Math.Max(dp[i + 1, j], dp[i, j + 1]);

            var start = new double[n];
            var end = new double[n];
            var matched = new bool[n];
            var count = 0;
            for (int i = 0, j = 0; i < n && j < m;)
            {
                if (a[i].Length > 0 && a[i] == b[j]) { matched[i] = true; start[i] = asr[j].Start; end[i] = Math.Max(asr[j].End, asr[j].Start); count++; i++; j++; }
                else if (dp[i + 1, j] >= dp[i, j + 1]) i++;
                else j++;
            }

            if (count == 0 || (double)count / n < MinMatchRatio) return null;

            // Eşleşmeyen kelimelere zaman ver: iki eşleşme arasına harf sayısına orantılı, uçlara sabit hızla
            var firstMatched = Array.FindIndex(matched, x => x);
            var lastMatched = Array.FindLastIndex(matched, x => x);

            for (var i = firstMatched - 1; i >= 0; i--)
            {
                end[i] = start[i + 1];
                start[i] = Math.Max(0, end[i] - FallbackWordSec);
            }
            for (var i = lastMatched + 1; i < n; i++)
            {
                start[i] = end[i - 1];
                end[i] = start[i] + FallbackWordSec;
            }

            var index = firstMatched;
            while (index < lastMatched)
            {
                if (matched[index]) { index++; continue; }
                var gapStart = index;
                while (!matched[index]) index++;
                var from = end[gapStart - 1];
                var to = Math.Max(from, start[index]);
                var weights = Enumerable.Range(gapStart, index - gapStart)
                    .Select(k => Math.Max(Norm(clipWords[k].Text).Length, 1)).ToList();
                var total = weights.Sum();
                var cursor = from;
                for (var k = gapStart; k < index; k++)
                {
                    var length = (to - from) * weights[k - gapStart] / total;
                    start[k] = cursor;
                    end[k] = cursor + length;
                    cursor += length;
                }
            }

            return Enumerable.Range(0, n)
                .Select(i => new CaptionWord(clipWords[i].Text, Math.Round(start[i], 2), Math.Round(Math.Max(end[i], start[i]), 2)))
                .ToList();
        }
    }
}
