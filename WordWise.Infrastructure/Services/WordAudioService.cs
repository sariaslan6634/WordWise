using System.Collections.Concurrent;
using System.Text.Json;
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
        private readonly ConcurrentDictionary<string, Task<WordAudioEntry?>> _inflight = new();

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

            // Aynı kelime için eşzamanlı istekler tek indirmeyi paylaşır. İndirme, istemci vazgeçse (bağlantıyı kapatsa) da
            // tamamlanır ki sonraki tıklamada dosya hazır olsun.
            var task = _inflight.GetOrAdd(missKey, _ => Task.Run(() => FetchAsync(key, accent, folder, missKey)));
            try { return await task.WaitAsync(cancellationToken); }
            catch (OperationCanceledException) { return null; }
            finally { if (task.IsCompleted) _inflight.TryRemove(missKey, out _); }
        }

        private async Task<WordAudioEntry?> FetchAsync(string key, string accent, string folder, string missKey)
        {
            var cancellationToken = CancellationToken.None;

            var baseName = key.Replace(' ', '_');
            var names = accent == WordAudioIndex.Us ? new[] { $"En-us-{baseName}.ogg" } : new[] { $"En-gb-{baseName}.ogg", $"En-uk-{baseName}.ogg" };
            try
            {
                var client = _httpFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(4);
                foreach (var name in names)
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, "https://commons.wikimedia.org/wiki/Special:FilePath/" + Uri.EscapeDataString(name));
                    request.Headers.UserAgent.ParseAdd("WordWise/1.0 (English learning app; pronunciation cache)");
                    using var response = await client.SendAsync(request, cancellationToken);
                    if (!response.IsSuccessStatusCode) { _logger.LogInformation("[WordAudio] Commons {Name}: HTTP {Status}", name, (int)response.StatusCode); continue; }
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

                _logger.LogInformation("[WordAudio] {Word} ({Accent}) Commons'ta bulunamadı; Free Dictionary deneniyor.", key, accent);
                var free = await TryFreeDictionaryAsync(client, key, accent, cancellationToken);
                if (free is not null)
                {
                    var dir = Path.Combine(folder, "fetched");
                    Directory.CreateDirectory(dir);
                    var path = Path.Combine(dir, $"En-{accent}-{baseName}{free.Value.Ext}");
                    await File.WriteAllBytesAsync(path, free.Value.Bytes, cancellationToken);
                    var entry = WordAudioIndex.Parse(path);
                    if (entry is not null)
                    {
                        _index.Value.Add(entry);
                        _logger.LogInformation("[WordAudio] {Word} ({Accent}) Free Dictionary'den indirildi.", key, accent);
                        return _index.Value.Find(key, accent);
                    }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or JsonException)
            {
                _logger.LogWarning("[WordAudio] {Word} ({Accent}) internetten alınamadı: {Error}", key, accent, ex.Message);
                // Ağ hatası/zaman aşımı: 10 dakika boyunca tekrar deneme (her tıklamada kullanıcıyı bekletme).
                _misses[missKey] = DateTime.UtcNow - MissRetry + TimeSpan.FromMinutes(10);
                return null;
            }

            _misses[missKey] = DateTime.UtcNow;
            return null;
        }


        private static readonly string[] AllowedAudioHosts = { "api.dictionaryapi.dev", "upload.wikimedia.org", "ssl.gstatic.com" };

        /// <summary>Free Dictionary API'nin verdiği ses bağlantısını (Amerikan: "-us", İngiliz: "-uk") indirir.</summary>
        private async Task<(byte[] Bytes, string Ext)?> TryFreeDictionaryAsync(HttpClient client, string word, string accent, CancellationToken ct)
        {
            using var response = await client.GetAsync("https://api.dictionaryapi.dev/api/v2/entries/en/" + Uri.EscapeDataString(word), ct);
            if (!response.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;

            var urls = new List<string>();
            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                if (!entry.TryGetProperty("phonetics", out var phonetics) || phonetics.ValueKind != JsonValueKind.Array) continue;
                foreach (var phonetic in phonetics.EnumerateArray())
                    if (phonetic.TryGetProperty("audio", out var audio) && audio.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(audio.GetString()))
                        urls.Add(audio.GetString()!.Trim());
            }

            bool Has(string url, params string[] tags) => tags.Any(t => url.Contains(t, StringComparison.OrdinalIgnoreCase));
            var chosen = accent == WordAudioIndex.Us
                ? urls.FirstOrDefault(u => Has(u, "-us.", "/us/")) ?? urls.FirstOrDefault(u => !Has(u, "-uk.", "-gb.", "-au.", "/uk/", "/gb/", "/au/"))
                : urls.FirstOrDefault(u => Has(u, "-uk.", "-gb.", "/uk/", "/gb/"));
            if (chosen is null) return null;
            if (chosen.StartsWith("//")) chosen = "https:" + chosen;
            if (!Uri.TryCreate(chosen, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !AllowedAudioHosts.Contains(uri.Host)) return null;

            var ext = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
            if (ext is not (".mp3" or ".ogg" or ".oga")) ext = ".mp3";
            using var audioResponse = await client.GetAsync(uri, ct);
            if (!audioResponse.IsSuccessStatusCode) return null;
            var bytes = await audioResponse.Content.ReadAsByteArrayAsync(ct);
            return bytes.Length is < 500 or > 3_000_000 ? null : (bytes, ext);
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
