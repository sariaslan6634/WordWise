using WordWise.Application.Features.WordAudio;

namespace WordWise.Application.Common.Interfaces
{
    public interface IWordAudioService
    {
        /// <summary>Kelimenin ses dosyası olan aksanları ("us", "gb").</summary>
        IReadOnlyList<string> GetAccents(string word);

        WordAudioEntry? Find(string word, string accent);
    }
}
