using System.Collections.Concurrent;
using System.Text.RegularExpressions;
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
        private static readonly Regex SafeWord = new(@"^[a-z0-9][a-z0-9' \-]{0,39}$", RegexOptions.Compiled);
        private static readonly TimeSpan MissRetry = TimeSpan.FromHours(6);

        private readonly Lazy<WordAudioIndex> _index;
        private readonly Lazy<string?> _folder;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<WordAudioService> _logger;
        private readonly ConcurrentDictionary<string, DateTime> _misses = new();

        public WordAudioService(IOptions<WordAudioSettings> options, ILogger<WordAudioService> logger, IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
            _logger = logger;
            _folder = new Lazy<string?>(() => ResolveFolder(options.Value.Folder), LazyThreadSafetyMode.ExecutionAndPublication);
            _index = new Lazy<WordAudioIndex>(() =>
            {
                var folder = _folder.Value;
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

        /// <summary>
        /// Yerel klasörde olmayan kelimenin kaydını Wikimedia Commons'tan (Wiktionary'nin ses kaynağı) indirir ve
        /// klasörün "fetched" alt klasörüne kaydeder; sonraki istekler yerelden sunulur. Bulunamayanlar 6 saat tekrar denenmez.
        /// </summary>
        public async Task<WordAudioEntry?> FindOrFetchAsync(string word, string accent, CancellationToken cancellationToken = default)
        {
            var local = Find(word, accent);
            if (local is not null) return local;

            var folder = _folder.Value;
            var key = word.Trim().ToLowerInvariant();
            if (folder is null || !SafeWord.IsMatch(key)) return null;

            var missKey = $"{accent}:{key}";
            if (_misses.TryGetValue(missKey, out var when) && DateTime.UtcNow - when < MissRetry) return null;

            var baseName = key.Replace(' ', '_');
            var names = accent == WordAudioIndex.Us ? new[] { $"En-us-{baseName}.ogg" } : new[] { $"En-gb-{baseName}.ogg", $"En-uk-{baseName}.ogg" };
            try
            {
                var client = _httpFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(8);
                foreach (var name in names)
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, "https://commons.wikimedia.org/wiki/Special:FilePath/" + Uri.EscapeDataString(name));
                    request.Headers.UserAgent.ParseAdd("WordWise/1.0 (English learning app; pronunciation cache)");
                    using var response = await client.SendAsync(request, cancellationToken);
                    if (!response.IsSuccessStatusCode) continue;
                    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    if (bytes.Length < 500 || bytes.Length > 3_000_000) continue;

                    var dir = Path.Combine(folder, "fetched");
                    Directory.CreateDirectory(dir);
                    var path = Path.Combine(dir, name);
                    await File.WriteAllBytesAsync(path, bytes, cancellationToken);
                    var entry = WordAudioIndex.Parse(path);
                    if (entry is null) continue;
                    _index.Value.Add(entry);
                    _logger.LogInformation("[WordAudio] {Name} Commons'tan indirildi.", name);
                    return _index.Value.Find(key, accent);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
            {
                _logger.LogDebug(ex, "[WordAudio] {Word} ({Accent}) Commons'tan alınamadı.", key, accent);
                return null; // geçici hata: olumsuz önbelleğe alma
            }

            _misses[missKey] = DateTime.UtcNow;
            return null;
        }

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
