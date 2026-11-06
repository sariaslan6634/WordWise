namespace WordWise.Domain.Enums
{
    public enum ClipReportReason
    {
        Quality = 1,        // Video kalitesi kötü
        CaptionSync = 2,    // Altyazı videoyla uyuşmuyor
        Inappropriate = 3,  // Uygunsuz içerik
        Other = 4
    }
}
