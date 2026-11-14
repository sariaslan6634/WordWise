using System.Text.Json;
using WordWise.Application.Features.Clips.Models;
using WordWise.Domain.Entities;
using WordWise.Domain.Enums;

namespace WordWise.Application.Features.Clips.Dtos
{
    public class VideoClipDto
    {
        public Guid Id { get; set; }
        public Guid? VideoCandidateId { get; set; }
        public string? SourceTitle { get; set; }
        public string YoutubeId { get; set; } = string.Empty;
        public double StartSec { get; set; }
        public double EndSec { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<CaptionWord> Words { get; set; } = new();
        public bool IsAutoCaption { get; set; }
        public bool IsPublished { get; set; }
        public bool IsRejected { get; set; }
        public bool HasBurnedInCaptions { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>"Passed" (uygun) veya "Failed" (uygun değil).</summary>
        public string QcStatus { get; set; } = string.Empty;
        public List<string> QcReasons { get; set; } = new();
        public List<string> QcWarnings { get; set; } = new();
    }

    public class GenerateClipsFromVideoDto
    {
        /// <summary>YouTube bağlantısı (watch?v=, youtu.be, shorts) veya 11 karakterlik video kimliği.</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>"auto" (varsayılan): en çok 5 klip; "all": videonun tamamı (en fazla 1 saatlik videolar).</summary>
        public string Mode { get; set; } = "auto";
    }

    public static class VideoClipMapper
    {
        public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public static VideoClipDto ToDto(VideoClip clip) => new()
        {
            Id = clip.Id,
            VideoCandidateId = clip.VideoCandidateId,
            SourceTitle = clip.SourceTitle,
            YoutubeId = clip.YoutubeId,
            StartSec = clip.StartSec,
            EndSec = clip.EndSec,
            Text = clip.Text,
            Words = JsonSerializer.Deserialize<List<CaptionWord>>(clip.WordTimingsJson, JsonOptions) ?? new(),
            IsAutoCaption = clip.IsAutoCaption,
            IsPublished = clip.IsPublished,
            IsRejected = clip.IsRejected,
            HasBurnedInCaptions = clip.HasBurnedInCaptions,
            CreatedAt = clip.CreatedAt,
            QcStatus = clip.QcStatus == ClipQcStatus.Passed ? "Passed" : clip.QcStatus == ClipQcStatus.Failed ? "Failed" : "Pending",
            QcReasons = SplitLines(clip.QcReasons),
            QcWarnings = SplitLines(clip.QcWarnings),
        };

        private static List<string> SplitLines(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : value.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
