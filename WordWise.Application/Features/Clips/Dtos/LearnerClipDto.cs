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

        /// <summary>Kaynak video bilgileri (altında gösterilir). Eski kliplerde kanal/açıklama boş olabilir.</summary>
        public string? SourceTitle { get; set; }
        public string? SourceChannel { get; set; }

        /// <summary>Kullanıcı bu klibi kaydetmiş mi?</summary>
        public bool IsSaved { get; set; }

        /// <summary>Kayıtlı klipler listesinde kaydedilme zamanı.</summary>
        public DateTime? SavedAt { get; set; }
    }

    public static class LearnerClipMapper
    {
        public static LearnerClipDto ToDto(WordWise.Domain.Entities.VideoClip c, bool isSaved, DateTime? savedAt = null) => new()
        {
            Id = c.Id,
            YoutubeId = c.YoutubeId,
            StartSec = c.StartSec,
            EndSec = c.EndSec,
            Text = c.Text,
            HasBurnedInCaptions = c.HasBurnedInCaptions,
            SourceTitle = c.SourceTitle,
            SourceChannel = c.SourceChannel,
            IsSaved = isSaved,
            SavedAt = savedAt,
            Words = System.Text.Json.JsonSerializer.Deserialize<List<CaptionWord>>(c.WordTimingsJson, VideoClipMapper.JsonOptions) ?? new(),
        };
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
