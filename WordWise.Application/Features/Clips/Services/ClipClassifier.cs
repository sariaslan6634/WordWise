using System.Text.RegularExpressions;

namespace WordWise.Application.Features.Clips.Services
{
    public enum ClipCategory
    {
        General = 0,
        Song = 1,
        TongueTwister = 2,
        SetPhrase = 3,
    }

    /// <summary>
    /// Kural tabanlı klip sınıflandırıcı (veritabanında saklanmaz; metinden ve video başlığından hesaplanır):
    /// şarkı, tekerleme, kalıp cümle veya genel. Sıra: başlıkta tekerleme > başlıkta şarkı > metinde tekerleme > kalıp cümle > genel.
    /// </summary>
    public static class ClipClassifier
    {
        private static readonly Regex TwisterTitle = new(@"\btongue[- ]?twisters?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SongTitle = new(
            @"\b(lyrics?|lyric video|official (music )?video|official audio|music video|karaoke|songs?)\b|\b(ft|feat)\b\.?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex NonWord = new(@"[^a-z0-9']+", RegexOptions.Compiled);

        private static readonly string[] KnownTwisters =
        {
            "peter piper", "seashells", "sea shells", "woodchuck", "betty botter", "how much wood",
            "red lorry", "unique new york", "fuzzy wuzzy",
        };

        private static readonly HashSet<string> Stopwords = new()
        {
            "the", "a", "an", "and", "or", "of", "to", "in", "on", "at", "by", "for", "with", "is", "was", "were", "are",
            "be", "it", "its", "he", "she", "we", "they", "them", "this", "that", "there", "then", "than", "but", "so",
            "as", "if", "you", "your", "his", "her", "i", "my", "me", "do", "did", "does", "has", "have", "had",
        };

        private static readonly string[] Phrases =
        {
            // Selamlaşma ve günlük konuşma
            "how are you", "how are you doing", "how's it going", "what's up", "nice to meet you", "good to see you",
            "long time no see", "see you later", "see you soon", "see you tomorrow", "take care", "have a nice day",
            "good morning", "good afternoon", "good evening", "good night", "thank you", "thanks a lot", "thank you very much",
            "you're welcome", "no problem", "my pleasure", "excuse me", "i'm sorry", "sorry about that", "pardon me",
            "bless you", "congratulations", "good luck", "best wishes", "welcome back", "make yourself at home",
            "help yourself", "after you", "go ahead", "here you are", "here you go", "never mind", "no worries",
            "don't worry", "take it easy", "i don't know", "i don't mind", "i see", "i agree", "that's right",
            "that makes sense", "of course", "sure thing", "i'd love to", "let me think", "let's go", "let's see",
            "what do you mean", "what's wrong", "what happened", "what's the matter", "how much is it", "how old are you",
            "where are you from", "what's your name", "my name is", "can you help me", "could you repeat that",
            "can you speak slowly", "i don't understand", "i'm looking for", "i'd like to", "would you like", "do you mind",
            "what time is it", "how do you say", "what does it mean", "it's up to you", "it depends", "you bet", "no way",
            "oh my god", "oh my gosh", "come on", "hold on", "hang on", "wait a minute", "just a moment", "hurry up",
            "calm down", "be careful", "watch out", "look out", "keep in touch", "i miss you", "i love you", "me too",
            "same here", "me neither",
            // Deyimler
            "piece of cake", "break a leg", "break the ice", "hit the sack", "under the weather", "once in a blue moon",
            "a blessing in disguise", "better late than never", "actions speak louder than words", "the ball is in your court",
            "cost an arm and a leg", "spill the beans", "let the cat out of the bag", "bite the bullet", "burn the midnight oil",
            "cut corners", "get out of hand", "hit the nail on the head", "kill two birds with one stone", "last straw",
            "on the fence", "pull someone's leg", "speak of the devil", "the best of both worlds", "time flies",
            "when pigs fly", "you can't judge a book by its cover", "raining cats and dogs", "call it a day",
            "go the extra mile", "in the same boat", "jump on the bandwagon", "miss the boat", "no pain no gain",
            "out of the blue", "see eye to eye", "grain of salt", "the elephant in the room", "throw in the towel",
            "up in the air", "barking up the wrong tree", "beat around the bush", "back to square one", "bend over backwards",
            "easier said than done", "keep an eye on", "make up your mind", "take your time", "think outside the box",
            "word of mouth", "a dime a dozen", "add insult to injury", "devil's advocate", "play it by ear", "rule of thumb",
            "sit tight", "the whole nine yards", "wrap your head around",
        };

        private static readonly (string Padded, int Words)[] PhraseIndex = Phrases
            .Select(p => (" " + p + " ", p.Split(' ').Length))
            .ToArray();

        public static ClipCategory Classify(string text, string? sourceTitle)
        {
            var title = sourceTitle ?? string.Empty;
            if (TwisterTitle.IsMatch(title)) return ClipCategory.TongueTwister;
            if (SongTitle.IsMatch(title)) return ClipCategory.Song;

            var normalized = Normalize(text);
            if (normalized.Length == 0) return ClipCategory.General;
            if (LooksLikeTongueTwister(normalized)) return ClipCategory.TongueTwister;

            var padded = " " + normalized + " ";
            var wordCount = normalized.Split(' ').Length;
            foreach (var (phrase, words) in PhraseIndex)
                if (padded.Contains(phrase, StringComparison.Ordinal) && wordCount <= words + 4)
                    return ClipCategory.SetPhrase;

            return ClipCategory.General;
        }

        /// <summary>API/arayüzde kullanılan kısa anahtar.</summary>
        public static string Key(ClipCategory category) => category switch
        {
            ClipCategory.Song => "song",
            ClipCategory.TongueTwister => "twister",
            ClipCategory.SetPhrase => "phrase",
            _ => "general",
        };

        /// <summary>"song,twister,phrase" gibi bir listeyi kategorilere çevirir; boş/"all" = filtre yok (null).</summary>
        public static IReadOnlyCollection<ClipCategory>? ParseFilter(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var result = new HashSet<ClipCategory>();
            foreach (var token in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                switch (token.ToLowerInvariant())
                {
                    case "all": return null;
                    case "song": result.Add(ClipCategory.Song); break;
                    case "twister": result.Add(ClipCategory.TongueTwister); break;
                    case "phrase": result.Add(ClipCategory.SetPhrase); break;
                    case "general": result.Add(ClipCategory.General); break;
                }
            }
            return result.Count == 0 ? null : result;
        }

        private static string Normalize(string text) =>
            NonWord.Replace(text.ToLowerInvariant().Replace('’', '\''), " ").Trim();

        private static bool LooksLikeTongueTwister(string normalized)
        {
            var padded = " " + normalized + " ";
            if (KnownTwisters.Any(k => padded.Contains(k, StringComparison.Ordinal))) return true;

            // Aynı harfle başlayan içerik kelimeleri baskınsa (Peter Piper picked a peck of pickled peppers...).
            var content = normalized.Split(' ').Where(w => w.Length > 2 && !Stopwords.Contains(w)).ToList();
            if (content.Count < 4) return false;
            var top = content.GroupBy(w => w[0]).Max(g => g.Count());
            return top >= 4 && top / (double)content.Count >= 0.7;
        }
    }
}
