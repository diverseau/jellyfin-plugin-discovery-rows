using MediaBrowser.Controller;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DiscoveryRows.Services;

public sealed class BackgroundWorker(IServerApplicationHost host, LibrarySnapshotService index, HssBridge hss,
    RecommendationService recommendations, ITaskManager tasks, ILogger<BackgroundWorker> logger) : BackgroundService
{
    // Jellyfin's "Scan media library" task. A rebuild after every scan catches changes that do not raise item events.
    private const string LibraryScanKey = "RefreshLibrary";

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        // HSS caches the available section types on the first homepage request.
        // Register before the web host starts serving requests, even though the
        // library index must wait for Jellyfin's later core startup completion.
        if (Plugin.Instance is not null) hss.Register();
        tasks.TaskCompleted += OnTaskCompleted;
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        tasks.TaskCompleted -= OnTaskCompleted;
        return base.StopAsync(cancellationToken);
    }

    private void OnTaskCompleted(object? sender, TaskCompletionEventArgs args)
    {
        if (args.Task.ScheduledTask.Key != LibraryScanKey) return;
        logger.LogInformation("Discovery Rows: library scan finished, rebuilding the local index");
        index.MarkDirty();
    }

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
                    // The first pass after every server start builds the index (the empty snapshot is infinitely old).
                    if (index.Dirty || DateTimeOffset.UtcNow - index.Snapshot.At > TimeSpan.FromHours(6))
                    { await index.RefreshAsync(stoppingToken).ConfigureAwait(false); recommendations.Invalidate(); }
                }
                await Task.Delay(TimeSpan.FromSeconds(host.CoreStartupHasCompleted ? 15 : 1), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Discovery Rows background maintenance failed"); await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken).ConfigureAwait(false); }
        }
    }
}
