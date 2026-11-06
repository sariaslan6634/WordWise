using WordWise.Domain.Enums;

namespace WordWise.Domain.Entities
{
    /// <summary>Öğrencinin bir klibi bildirmesi. Kullanıcı başına klip başına tek kayıt tutulur.</summary>
    public class ClipReport : BaseEntity
    {
        public Guid ClipId { get; set; }
        public VideoClip Clip { get; set; } = null!;

        public Guid UserId { get; set; }

        public ClipReportReason Reason { get; set; }
        public string? Note { get; set; }

        /// <summary>"Open" (admin incelemesi bekliyor) veya "Resolved".</summary>
        public string Status { get; set; } = "Open";
    }
}
