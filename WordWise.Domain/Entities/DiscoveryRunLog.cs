namespace WordWise.Domain.Entities
{
    /// <summary>Admin panelinden "Klip getir" butonuyla yapılan her keşif çalıştırması; YouTube kotasını korumak için günlük tıklama hakkını saymakta ve ne kadar video/klip çekildiğini göstermekte kullanılır.</summary>
    public class DiscoveryRunLog : BaseEntity
    {
        public int VideosProcessed { get; set; }
        public int ClipsCreated { get; set; }
        public int ClipsPublished { get; set; }
        public int FailedVideos { get; set; }
    }
}
