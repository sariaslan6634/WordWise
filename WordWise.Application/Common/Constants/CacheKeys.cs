using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWise.Application.Common.Constants
{
    public static class CacheKeys
    {
        public const string WordPrefix = "word:";
        public const string VideoPrefix = "videos:";
        public const string DailyChallengePrefix = "daily:";

        public static string WordByText(string text)
            => $"{WordPrefix}{text.ToLowerInvariant().Trim()}";

        public static string VideosByWordId(Guid wordId)
            => $"{VideoPrefix}{wordId}";

        public static string DailyChallenge(DateOnly date)
            => $"{DailyChallengePrefix}{date:yyyy-MM-dd}";

        public static readonly TimeSpan WordTtl = TimeSpan.FromMinutes(60);
        public static readonly TimeSpan VideoTtl = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan DailyChallengeTtl = TimeSpan.FromHours(24);
    }
}
