using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Common.Settings;
using WordWise.Application.Features.WordAudio;

namespace WordWise.Infrastructure.Services
{
    /// <summary>Ses klasörünü ilk kullanımda bir kez tarar ve bellekte tutar (30 bin dosya birkaç yüz ms sürer).</summary>
    public class WordAudioService : IWordAudioService
    {
        private readonly Lazy<WordAudioIndex> _index;

        public WordAudioService(IOptions<WordAudioSettings> options, ILogger<WordAudioService> logger)
        {
            _index = new Lazy<WordAudioIndex>(() =>
            {
                var folder = ResolveFolder(options.Value.Folder);
                if (folder is null)
                {
                    logger.LogWarning("[WordAudio] Ses klasörü bulunamadı (appsettings: WordAudio:Folder veya proje klasöründe Data/wiktionary-audios). Ses butonları pasif kalacak.");
                    return WordAudioIndex.Build(Array.Empty<string>());
                }
                var index = WordAudioIndex.Build(Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories));
                logger.LogInformation("[WordAudio] {Folder} klasöründen {Count} (kelime, aksan) kaydı yüklendi.", folder, index.Count);
                return index;
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public IReadOnlyList<string> GetAccents(string word) => _index.Value.AccentsFor(word);

        public WordAudioEntry? Find(string word, string accent) => _index.Value.Find(word, accent);

        private static string? ResolveFolder(string configured)
        {
            if (!string.IsNullOrWhiteSpace(configured))
                return Directory.Exists(configured) ? configured : null;

            foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                var dir = new DirectoryInfo(start);
                while (dir is not null)
                {
                    var candidate = Path.Combine(dir.FullName, "Data", "wiktionary-audios");
                    if (Directory.Exists(candidate)) return candidate;
                    dir = dir.Parent;
                }
            }
            return null;
        }
    }
}
