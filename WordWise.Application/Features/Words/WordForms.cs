using System.Text.RegularExpressions;

namespace WordWise.Application.Features.Words
{
    /// <summary>
    /// Altyazıda tıklanan çekimli bir sözcüğün (giving, cities, don't...) sözlükteki olası ana biçimlerini üretir.
    /// Sözlükte yalnızca ana maddeler (give, city, do...) olduğu için arama bu adaylarla sırayla yapılır.
    /// </summary>
    public static class WordForms
    {
        private static readonly Regex EdgeRegex = new(@"^[^\p{L}\p{N}]+|[^\p{L}\p{N}]+$", RegexOptions.Compiled);

        public static string Clean(string raw) =>
            EdgeRegex.Replace((raw ?? string.Empty).Replace('’', '\''), string.Empty).ToLowerInvariant();

        /// <summary>Önce sözcüğün kendisi, sonra olası ana biçimler (tekrarsız, kısadan uzuna güvenilirlik sırasıyla).</summary>
        public static List<string> Candidates(string raw)
        {
            var word = Clean(raw);
            var result = new List<string>();
            void Add(string? value)
            {
                if (string.IsNullOrEmpty(value) || value.Length < 2 && value != "a" && value != "i") return;
                if (!result.Contains(value)) result.Add(value);
            }

            Add(word);
            if (word.Length == 0) return result;

            // Kısaltmalar ve iyelik: you're → you, it's → it, don't → do, can't → can
            var apostrophe = word.IndexOf('\'');
            if (apostrophe > 0)
            {
                var head = word[..apostrophe];
                var tail = word[(apostrophe + 1)..];
                Add(word.Replace("'", string.Empty));
                if (head.EndsWith("n") && tail == "t") Add(head[..^1] == "ca" ? "can" : head[..^1]);
                Add(head);
                return result;
            }

            string Undouble(string stem) => stem.Length >= 3 && stem[^1] == stem[^2] ? stem[..^1] : stem;

            if (word.EndsWith("ies") && word.Length > 4) Add(word[..^3] + "y");
            if (word.EndsWith("ied") && word.Length > 4) Add(word[..^3] + "y");
            if (word.EndsWith("ing") && word.Length > 4)
            {
                var stem = word[..^3];
                Add(stem); Add(stem + "e"); Add(Undouble(stem));
            }
            if (word.EndsWith("ed") && word.Length > 3)
            {
                var stem = word[..^2];
                Add(stem); Add(stem + "e"); Add(Undouble(stem)); Add(word[..^1]);
            }
            if (word.EndsWith("es") && word.Length > 3) { Add(word[..^2]); Add(word[..^1]); }
            else if (word.EndsWith("s") && !word.EndsWith("ss") && word.Length > 2) Add(word[..^1]);
            if (word.EndsWith("ier") && word.Length > 4) Add(word[..^3] + "y");
            else if (word.EndsWith("er") && word.Length > 3) { var stem = word[..^2]; Add(stem); Add(stem + "e"); Add(Undouble(stem)); }
            if (word.EndsWith("iest") && word.Length > 5) Add(word[..^4] + "y");
            else if (word.EndsWith("est") && word.Length > 4) { var stem = word[..^3]; Add(stem); Add(stem + "e"); Add(Undouble(stem)); }
            if (word.EndsWith("ly") && word.Length > 4) Add(word[..^2]);

            return result;
        }
    }
}
