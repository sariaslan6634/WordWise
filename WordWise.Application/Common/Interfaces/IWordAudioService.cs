using WordWise.Application.Features.WordAudio;

namespace WordWise.Application.Common.Interfaces
{
    public interface IWordAudioService
    {
        /// <summary>Kelimenin ses dosyası olan aksanları ("us", "gb").</summary>
        IReadOnlyList<string> GetAccents(string word);

        WordAudioEntry? Find(string word, string accent);

        /// <summary>Yerel klasörde yoksa kaydı Wikimedia Commons'tan indirip klasöre önbellekler. Bulunamazsa null.</summary>
        Task<WordAudioEntry?> FindOrFetchAsync(string word, string accent, CancellationToken cancellationToken = default) =>
            Task.FromResult(Find(word, accent));
    }
}
