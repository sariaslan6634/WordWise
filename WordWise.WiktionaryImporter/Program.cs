using System.Data;
using Microsoft.Data.SqlClient;
using WordWise.WiktionaryImporter;

// Kullanım:
//   dotnet run --project WordWise.WiktionaryImporter -c Release -- --file "D:\raw-wiktextract-data.jsonl" --connection "<bağlantı>" [--limit 50000] [--wordlist top20k.txt] [--dry-run] [--publish]
// CEFR seviyesi doldurma modu: Wiktionary dosyası okunmaz, sadece Words.CefrLevel güncellenir.
if (args.Contains("--cefr-file") || args.Contains("--freq-file")) return CefrUpdater.Run(args);

var options = ImporterOptions.Parse(args);
if (options is null) return 1;

var cancelled = false;
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancelled = true; Console.WriteLine("\nDurduruluyor; mevcut paket yazılıp çıkılacak..."); };

HashSet<string>? allowed = null;
if (options.WordList is not null)
{
    allowed = File.ReadLines(options.WordList).Select(l => l.Trim().ToLowerInvariant()).Where(l => l.Length > 0)
        .ToHashSet(StringComparer.Ordinal);
    Console.WriteLine($"Kelime listesi: {allowed.Count:N0} kelime (sadece bunlar alınacak).");
}

var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
if (!options.DryRun)
{
    using var conn = new SqlConnection(options.Connection);
    conn.Open();
    using var cmd = new SqlCommand("SELECT [Text] FROM [Words]", conn);
    using var reader = cmd.ExecuteReader();
    while (reader.Read()) seen.Add(reader.GetString(0));
    Console.WriteLine($"Veritabanında zaten {seen.Count:N0} kelime var; bunlar atlanacak.");
}

var table = NewTable();
long lines = 0, accepted = 0, inserted = 0;
var started = DateTime.UtcNow;

using var file = new StreamReader(options.File, System.Text.Encoding.UTF8, true, 1 << 20);
string? line;
while (!cancelled && (line = file.ReadLine()) is not null)
{
    lines++;
    if (lines % 500_000 == 0)
        Console.WriteLine($"{lines:N0} satır okundu · {accepted:N0} kelime bulundu · {inserted:N0} yazıldı · {(DateTime.UtcNow - started):hh\\:mm\\:ss}");

    var word = WiktionaryParser.Parse(line);
    if (word is null) continue;
    if (allowed is not null && !allowed.Contains(word.Text)) continue;
    if (!seen.Add(word.Text)) continue;

    accepted++;
    if (options.DryRun)
    {
        if (accepted <= 10) Console.WriteLine($"  {word.Text} ({word.PartOfSpeech}) {word.Ipa} — {word.Definition}");
    }
    else
    {
        AddRow(table, word, options.Publish);
        if (table.Rows.Count >= options.Batch) inserted += Flush(table, options.Connection);
    }
    if (options.Limit > 0 && accepted >= options.Limit) break;
}

if (!options.DryRun && table.Rows.Count > 0) inserted += Flush(table, options.Connection);

Console.WriteLine($"Bitti. {lines:N0} satır okundu, {accepted:N0} yeni kelime bulundu, {inserted:N0} veritabanına yazıldı{(options.DryRun ? " (dry-run: hiçbir şey yazılmadı)" : "")}.");
return 0;

static DataTable NewTable()
{
    var t = new DataTable();
    t.Columns.Add("Id", typeof(Guid));
    t.Columns.Add("CreatedAt", typeof(DateTime));
    t.Columns.Add("UpdatedAt", typeof(DateTime));
    t.Columns.Add("IsDeleted", typeof(bool));
    t.Columns.Add("Text", typeof(string));
    t.Columns.Add("Definition", typeof(string));
    t.Columns.Add("Ipa", typeof(string));
    t.Columns.Add("PartOfSpeech", typeof(string));
    t.Columns.Add("CefrLevel", typeof(int));
    t.Columns.Add("ExampleSentencesJson", typeof(string));
    t.Columns.Add("SynonymsJson", typeof(string));
    t.Columns.Add("AntonymsJson", typeof(string));
    t.Columns.Add("Category", typeof(string));
    t.Columns.Add("SourceProvider", typeof(string));
    t.Columns.Add("SourceUrl", typeof(string));
    t.Columns.Add("ImportedAt", typeof(DateTime));
    t.Columns.Add("IsPublished", typeof(bool));
    t.Columns.Add("IsApproved", typeof(bool));
    return t;
}

static void AddRow(DataTable t, ImportedWord w, bool publish)
{
    var now = DateTime.UtcNow;
    var row = t.NewRow();
    row["Id"] = Guid.NewGuid();
    row["CreatedAt"] = now;
    row["UpdatedAt"] = DBNull.Value;
    row["IsDeleted"] = false;
    row["Text"] = w.Text;
    row["Definition"] = w.Definition;
    row["Ipa"] = (object?)w.Ipa ?? DBNull.Value;
    row["PartOfSpeech"] = w.PartOfSpeech;
    row["CefrLevel"] = DBNull.Value;
    row["ExampleSentencesJson"] = (object?)w.ExamplesJson ?? DBNull.Value;
    row["SynonymsJson"] = (object?)w.SynonymsJson ?? DBNull.Value;
    row["AntonymsJson"] = (object?)w.AntonymsJson ?? DBNull.Value;
    row["Category"] = DBNull.Value;
    row["SourceProvider"] = "Wiktionary";
    row["SourceUrl"] = $"https://en.wiktionary.org/wiki/{w.Text}";
    row["ImportedAt"] = now;
    row["IsPublished"] = publish;   // --publish verilmezse admin onayı beklenir
    row["IsApproved"] = publish;
    t.Rows.Add(row);
}

static int Flush(DataTable t, string connection)
{
    var count = t.Rows.Count;
    using var bulk = new SqlBulkCopy(connection) { DestinationTableName = "Words", BatchSize = 5000, BulkCopyTimeout = 0 };
    foreach (DataColumn c in t.Columns) bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);
    bulk.WriteToServer(t);
    t.Clear();
    return count;
}

internal sealed class ImporterOptions
{
    public string File { get; private set; } = "";
    public string Connection { get; private set; } = "";
    public string? WordList { get; private set; }
    public long Limit { get; private set; }
    public int Batch { get; private set; } = 5000;
    public bool DryRun { get; private set; }
    public bool Publish { get; private set; }

    public static ImporterOptions? Parse(string[] args)
    {
        var o = new ImporterOptions
        {
            Connection = Environment.GetEnvironmentVariable("WORDWISE_CONNECTION") ?? "",
        };
        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : "";
            switch (args[i])
            {
                case "--file": o.File = Next(); break;
                case "--connection": o.Connection = Next(); break;
                case "--wordlist": o.WordList = Next(); break;
                case "--limit": long.TryParse(Next(), out var l); o.Limit = l; break;
                case "--batch": int.TryParse(Next(), out var b); if (b > 0) o.Batch = b; break;
                case "--dry-run": o.DryRun = true; break;
                case "--publish": o.Publish = true; break;
            }
        }
        if (string.IsNullOrWhiteSpace(o.File) || !System.IO.File.Exists(o.File))
        {
            Console.WriteLine("Hata: --file ile var olan bir .jsonl dosyası verin.");
            return null;
        }
        if (o.WordList is not null && !System.IO.File.Exists(o.WordList))
        {
            Console.WriteLine("Hata: --wordlist dosyası bulunamadı.");
            return null;
        }
        if (!o.DryRun && string.IsNullOrWhiteSpace(o.Connection))
        {
            Console.WriteLine("Hata: --connection verin (veya WORDWISE_CONNECTION ortam değişkenini ayarlayın). Deneme için --dry-run kullanabilirsiniz.");
            return null;
        }
        return o;
    }
}
