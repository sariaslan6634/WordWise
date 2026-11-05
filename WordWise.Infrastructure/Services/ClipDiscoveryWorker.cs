using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WordWise.Application.Common.Settings;
using WordWise.Application.Features.Clips.Services;

namespace WordWise.Infrastructure.Services
{
    /// <summary>AutoDiscovery:Enabled true ise belirli aralıklarla keşif ajanını çalıştırır.</summary>
    public class ClipDiscoveryWorker(
        IServiceScopeFactory _scopeFactory,
        IOptions<AutoDiscoverySettings> _options,
        ILogger<ClipDiscoveryWorker> _logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Value.Enabled)
            {
                _logger.LogInformation("Otomatik klip keşfi kapalı (AutoDiscovery:Enabled=false).");
                return;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // API ayağa kalksın
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var runner = scope.ServiceProvider.GetRequiredService<ClipDiscoveryRunner>();
                        var result = await runner.RunOnceAsync(stoppingToken);
                        _logger.LogInformation(
                            "Otomatik keşif tamamlandı. Sorgu: {Query}, bulunan: {Found}, işlenen: {Processed}, klip: {Clips}, yayınlanan: {Published}, hata: {Failures}",
                            result.Query, result.VideosFound, result.VideosProcessed, result.ClipsCreated, result.ClipsPublished, result.Failures.Count);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Otomatik keşif çalışması başarısız oldu.");
                    }

                    var minutes = Math.Max(5, _options.Value.IntervalMinutes);
                    await Task.Delay(TimeSpan.FromMinutes(minutes), stoppingToken);
                }
            }
            catch (OperationCanceledException) { /* uygulama kapanıyor */ }
        }
    }
}
