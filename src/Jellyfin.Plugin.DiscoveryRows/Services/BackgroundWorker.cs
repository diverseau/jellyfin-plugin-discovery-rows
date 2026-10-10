using MediaBrowser.Controller;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DiscoveryRows.Services;

public sealed class BackgroundWorker(IServerApplicationHost host, LibrarySnapshotService index, HssBridge hss,
    RecommendationService recommendations, ILogger<BackgroundWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var attempts = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (Plugin.Instance is not null && host.CoreStartupHasCompleted)
                {
                    if (hss.RegisteredCount < global::DiscoveryRows.Core.RowCatalogue.All.Count || !hss.LabelsReady || !hss.PreferencesPageReady)
                    { if (attempts++ < 20) hss.Register(); }
                    if (index.Dirty || DateTimeOffset.UtcNow - index.Snapshot.At > TimeSpan.FromHours(6))
                    { await index.RefreshAsync(stoppingToken).ConfigureAwait(false); recommendations.Invalidate(); }
                }
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Discovery Rows background maintenance failed"); await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken).ConfigureAwait(false); }
        }
    }
}
