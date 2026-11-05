using WordWise.Application.Features.Clips.Models;

namespace WordWise.Application.Common.Interfaces
{
    public interface ICaptionService
    {
        /// <summary>İngilizce altyazıyı (varsa elle girilmiş, yoksa otomatik) döner. Bulunamazsa null.</summary>
        Task<CaptionTrack?> GetCaptionsAsync(string youtubeId, CancellationToken cancellationToken = default);
    }

    public interface IVideoDetailsService
    {
        /// <summary>YouTube Data API'den kalite/izin/yaş sınırı bilgilerini döner. Bulunamazsa null.</summary>
        Task<VideoDetails?> GetVideoDetailsAsync(string youtubeId, CancellationToken cancellationToken = default);
    }
}
