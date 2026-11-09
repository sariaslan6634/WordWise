using System.Data;
using Microsoft.Data.SqlClient;

namespace WordWise.WiktionaryImporter;

/// <summary>
/// Words.CefrLevel alanını bir CEFR kelime listesinden (ve isteğe bağlı sıklık listesinden) doldurur.
/// Kullanım: --cefr-file cefrj.csv [--freq-file sıklık.txt] --connection "..." [--overwrite] [--dry-run]
/// </summary>
public static class CefrUpdater
{
    public static int Run(string[] args)
    {
        string? cefrFile = null, freqFile = null, connection = Environment.GetEnvironmentVariable("WORDWISE_CONNECTION");
        bool overwrite = false, dryRun = false;
        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : "";
            switch (args[i])
            {
                case "--cefr-file": cefrFile = Next(); break;
                case "--freq-file": freqFile = Next(); break;
                case "--connection": connection = Next(); break;
                case "--overwrite": overwrite = true; break;
                case "--dry-run": dryRun = true; break;
            }
        }
        foreach (var path in new[] { cefrFile, freqFile })
            if (path is not null && !File.Exists(path)) { Console.WriteLine($"Hata: dosya bulunamadı: {path}"); return 1; }
        if (cefrFile is null && freqFile is null) { Console.WriteLine("Hata: --cefr-file ve/veya --freq-file verin."); return 1; }
        if (!dryRun && string.IsNullOrWhiteSpace(connection)) { Console.WriteLine("Hata: --connection verin (veya WORDWISE_CONNECTION)."); return 1; }

        // Gerçek CEFR listesi önceliklidir; sıklık listesi sadece onun kapsamadığı kelimeleri tamamlar.
        var levels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (cefrFile is not null)
        {
            levels = CefrLevels.ParseCefrCsv(File.ReadLines(cefrFile));
            Console.WriteLine($"CEFR listesi: {levels.Count:N0} kelime.");
        }
        if (freqFile is not null)
        {
            var freq = CefrLevels.ParseFrequencyList(File.ReadLines(freqFile));
            var added = 0;
            foreach (var (word, level) in freq) if (levels.TryAdd(word, level)) added++;
            Console.WriteLine($"Sıklık listesi: {freq.Count:N0} kelime, bunlardan {added:N0} tanesi CEFR listesinde olmadığı için yaklaşık seviyeyle eklendi.");
        }

        if (dryRun)
        {
            foreach (var (word, level) in levels.Take(10)) Console.WriteLine($"  {word} → {level}");
            Console.WriteLine("dry-run: veritabanına yazılmadı.");
            return 0;
        }

        using var conn = new SqlConnection(connection);
        conn.Open();
        using (var create = new SqlCommand("CREATE TABLE #cefr ([Text] nvarchar(100) NOT NULL PRIMARY KEY, [Level] int NOT NULL)", conn))
            create.ExecuteNonQuery();

        var table = new DataTable();
        table.Columns.Add("Text", typeof(string));
        table.Columns.Add("Level", typeof(int));
        foreach (var (word, level) in levels)
            if (word.Length <= 100) table.Rows.Add(word, level);

        using (var bulk = new SqlBulkCopy(conn) { DestinationTableName = "#cefr", BulkCopyTimeout = 0 })
        {
            bulk.ColumnMappings.Add("Text", "Text");
            bulk.ColumnMappings.Add("Level", "Level");
            bulk.WriteToServer(table);
        }

        var sql = "UPDATE w SET w.CefrLevel = c.[Level], w.UpdatedAt = SYSUTCDATETIME() FROM Words w JOIN #cefr c ON w.[Text] = c.[Text]"
                  + (overwrite ? "" : " WHERE w.CefrLevel IS NULL");
        using var update = new SqlCommand(sql, conn) { CommandTimeout = 0 };
        var updated = update.ExecuteNonQuery();
        Console.WriteLine($"{updated:N0} kelimenin CEFR seviyesi güncellendi{(overwrite ? "" : " (sadece seviyesi boş olanlar)")}.");
        return 0;
    }
}
