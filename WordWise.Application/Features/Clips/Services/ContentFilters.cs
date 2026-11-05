using System.Text.RegularExpressions;

namespace WordWise.Application.Features.Clips.Services
{
    /// <summary>Metin tabanlı küfür ve yetişkin içerik taraması (kural tabanlı).</summary>
    public static class ContentFilters
    {
        private static readonly HashSet<string> ProfanityTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            "fuck","fucks","fucked","fucking","fucker","fuckers","motherfucker","motherfuckers",
            "shit","shits","shitty","bullshit","bitch","bitches","bastard","bastards",
            "asshole","assholes","ass","dick","dicks","cunt","cunts","pussy","whore","slut",
            "douche","douchebag","piss","pissed","crap","wtf","stfu","jackass",
            "nigger","nigga","faggot","fag","retard","retarded"
        };

        // Başka kelimelerin içinde görünmesi çok nadir olanlar ("cunt" ⊂ Scunthorpe, "nigg" ⊂ niggle gibi riskli kökler bilerek yok)
        private static readonly string[] ProfanityStems = { "fuck", "shit", "bitch" };

        private static readonly Regex TokenRegex = new(@"[\p{L}]+", RegexOptions.Compiled);
        private static readonly Regex CensoredRegex = new(@"(?<!\w)[A-Za-z]\*{2,}[A-Za-z]*", RegexOptions.Compiled);

        private static readonly Regex AdultRegex = new(
            @"(?<!\w)(?:porn|porno|pornography|pornhub|xxx|nsfw|nude|nudes|naked|topless|erotic|onlyfans|hentai|blowjob|orgasm|masturbat\w*|striptease|sex tape|18\+|adults? only)(?!\w)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>Bulunan küfürleri maskeli döner (örn. "f***").</summary>
        public static List<string> FindProfanity(string? text)
        {
            var found = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return found;

            foreach (Match m in TokenRegex.Matches(text))
            {
                var token = m.Value.ToLowerInvariant();
                if (ProfanityTokens.Contains(token) || ProfanityStems.Any(stem => token.Contains(stem)))
                    found.Add(Mask(token));
            }
            foreach (Match m in CensoredRegex.Matches(text))
                found.Add(m.Value);

            return found.Distinct().ToList();
        }

        public static List<string> FindAdultTerms(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            return AdultRegex.Matches(text).Select(m => m.Value.ToLowerInvariant()).Distinct().ToList();
        }

        private static string Mask(string word) => word.Length <= 1 ? word : word[0] + new string('*', word.Length - 1);
    }
}
