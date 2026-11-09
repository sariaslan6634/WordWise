using WordWise.Application.Features.Clips.Models;

namespace WordWise.Application.Features.Clips.Dtos
{
    /// <summary>Öğrenciye gösterilen klip: kalite kontrol bilgileri olmadan.</summary>
    public class LearnerClipDto
    {
        public Guid Id { get; set; }
        public string YoutubeId { get; set; } = string.Empty;
        public double StartSec { get; set; }
        public double EndSec { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<CaptionWord> Words { get; set; } = new();

        /// <summary>true ise videonun kendi altyazısı var; arayüz bizim altyazımızı göstermemeli.</summary>
        public bool HasBurnedInCaptions { get; set; }

        /// <summary>"general", "song", "twister" veya "phrase".</summary>
        public string Category { get; set; } = "general";
    }

    public class SetBurnedInCaptionsRequest
    {
        public bool Value { get; set; }
    }

    public class ReportClipRequest
    {
        /// <summary>Quality, CaptionSync, Inappropriate veya Other.</summary>
        public string Reason { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
