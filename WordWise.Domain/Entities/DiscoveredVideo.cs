namespace WordWise.Domain.Entities
{
    /// <summary>Otomatik keşif ajanının işlediği video kaydı; aynı video tekrar tekrar işlenmesin diye tutulur.</summary>
    public class DiscoveredVideo : BaseEntity
    {
        public string YoutubeId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string SearchQuery { get; set; } = string.Empty;

        /// <summary>"Done" veya "Failed".</summary>
        public string Status { get; set; } = "Done";
        public int ClipCount { get; set; }
        public int PublishedCount { get; set; }
        public string? Error { get; set; }
    }
}
