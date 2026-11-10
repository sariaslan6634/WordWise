namespace WordWise.Application.Features.UserWords.Dtos
{
    public class RemovedWordDto
    {
        public Guid WordId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? PartOfSpeech { get; set; }
        public DateTime RemovedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
