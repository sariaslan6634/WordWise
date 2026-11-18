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

        /// <summary>Her çalışmada (butona her tıklamada) işlenecek en çok yeni video sayısı. Az az çekmek için küçük tutulur; her video altyazı indirme/klip üretme süresi harcar.</summary>
        public int MaxVideosPerRun { get; set; } = 5;

        public int MaxResultsPerQuery { get; set; } = 10;

        /// <summary>Admin panelindeki "Klip getir" butonu için pencere başına toplam tıklama hakkı (YouTube kotası). Örn. 5.</summary>
        public int ManualRunLimit { get; set; } = 5;

        /// <summary>Bu kadar tıklama hakkı her zaman yedekte kalır; butonla kullanılabilen hak = ManualRunLimit - ManualRunReserve (varsayılan 5 - 1 = 4).</summary>
        public int ManualRunReserve { get; set; } = 1;

        /// <summary>Tıklama haklarının sayıldığı kayan pencere (saat).</summary>
        public int ManualRunWindowHours { get; set; } = 24;

        /// <summary>Bir videodan en çok kaç klip alınır (güvenlik sınırı; normalde videodaki tüm uygun cümleler alınır).</summary>
        public int MaxClipsPerVideo { get; set; } = 300;

        /// <summary>Bundan uzun videolar işlenmez (her klip bir veritabanı satırı; seçim/kalite kontrolü klip sayısıyla çok artar).</summary>
        public int MaxVideoMinutes { get; set; } = 60;

        /// <summary>true ise kalite kontrolünden geçen klipler admin onayı beklemeden yayınlanır.</summary>
        public bool AutoPublish { get; set; } = true;

        /// <summary>true ise yalnızca kelime zamanları gerçek (otomatik altyazı kaydıyla eşleşen) klipler otomatik yayınlanır; tahmini zamanlılar admin onayı bekler.</summary>
        public bool AutoPublishRequiresRealTimings { get; set; } = true;

        /// <summary>YouTube araması süre filtresi: "short" (4 dk altı), "medium" (4-20 dk), "long" (20 dk üstü), "any". Her videodan olabildiğince çok klip alındığı için varsayılan "medium".</summary>
        public string VideoDuration { get; set; } = "medium";

        /// <summary>Arama sorguları. Boşsa koddaki varsayılan liste kullanılır.</summary>
        public List<string> Queries { get; set; } = new();
    }
}
