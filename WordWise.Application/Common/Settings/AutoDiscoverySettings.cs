namespace WordWise.Application.Common.Settings
{
    /// <summary>Otomatik video bulma ve klip üretme ajanının ayarları (appsettings: "AutoDiscovery").</summary>
    public class AutoDiscoverySettings
    {
        public const string SectionName = "AutoDiscovery";

        /// <summary>false ise arka plan ajanı hiç çalışmaz (elle çalıştırma endpoint'i yine de kullanılabilir).</summary>
        public bool Enabled { get; set; } = false;

        /// <summary>İki otomatik çalışma arasındaki süre (dakika). En az 5.</summary>
        public int IntervalMinutes { get; set; } = 360;

        /// <summary>Her çalışmada işlenecek en çok yeni video sayısı.</summary>
        public int MaxVideosPerRun { get; set; } = 3;

        public int MaxResultsPerQuery { get; set; } = 10;

        /// <summary>true ise kalite kontrolünden geçen klipler admin onayı beklemeden yayınlanır.</summary>
        public bool AutoPublish { get; set; } = true;

        /// <summary>true ise yalnızca kelime zamanları gerçek (otomatik altyazı kaydıyla eşleşen) klipler otomatik yayınlanır; tahmini zamanlılar admin onayı bekler.</summary>
        public bool AutoPublishRequiresRealTimings { get; set; } = true;

        /// <summary>YouTube araması süre filtresi: "short" (4 dk altı), "medium" (4-20 dk), "any".</summary>
        public string VideoDuration { get; set; } = "short";

        /// <summary>Arama sorguları. Boşsa koddaki varsayılan liste kullanılır.</summary>
        public List<string> Queries { get; set; } = new();
    }
}
