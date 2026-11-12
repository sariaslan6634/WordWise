using WordWise.Domain.Enums;

namespace WordWise.Domain.Entities
{
    /// <summary>
    /// Bir video adayından seçilen tek cümlelik kesit. Klip seçici ajan üretir,
    /// kalite kontrolcü ajan QcStatus/QcReasons alanlarını doldurur.
    /// </summary>
    public class VideoClip : BaseEntity
    {
        /// <summary>Kelimeye bağlı aday videodan üretildiyse dolu; doğrudan YouTube bağlantısından üretildiyse boş.</summary>
        public Guid? VideoCandidateId { get; set; }
        public VideoCandidate? VideoCandidate { get; set; }

        public string? SourceTitle { get; set; }

        /// <summary>Videoyu yükleyen kanalın adı (öğrenciye kaynak olarak gösterilir).</summary>
        public string? SourceChannel { get; set; }

        /// <summary>Video açıklamasının ilk bölümü (en çok 1000 karakter).</summary>
        public string? SourceDescription { get; set; }

        public string YoutubeId { get; set; } = string.Empty;

        public double StartSec { get; set; }
        public double EndSec { get; set; }

        /// <summary>Videoda söylenen tek cümle.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>[{ "text": "...", "start": 12.3, "end": 12.7 }, ...] biçiminde kelime zamanları.</summary>
        public string WordTimingsJson { get; set; } = "[]";

        public bool IsAutoCaption { get; set; }

        /// <summary>Admin onayladı; öğrencilere gösterilir. Sadece kalite kontrolünden geçen klipler yayınlanabilir.</summary>
        public bool IsPublished { get; set; } = false;

        /// <summary>Videonun kendisinde yazılı altyazı var; bizim altyazımız gösterilmez.</summary>
        public bool HasBurnedInCaptions { get; set; } = false;

        public bool IsRejected { get; set; } = false;

        public ClipQcStatus QcStatus { get; set; } = ClipQcStatus.Pending;

        /// <summary>Uygun değilse nedenler (satır satır).</summary>
        public string? QcReasons { get; set; }

        /// <summary>Klibi engellemeyen ama admin'in bilmesi gereken notlar.</summary>
        public string? QcWarnings { get; set; }
    }
}
