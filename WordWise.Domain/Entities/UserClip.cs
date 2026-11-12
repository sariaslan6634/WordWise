namespace WordWise.Domain.Entities
{
    /// <summary>Öğrencinin kaydettiği klip. (UserId, ClipId) tekil; kaydı kaldırmak soft-delete'tir, yeniden kaydedince satır geri gelir.</summary>
    public class UserClip : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid ClipId { get; set; }
        public VideoClip Clip { get; set; } = null!;
    }
}
