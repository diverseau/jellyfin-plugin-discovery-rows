using Jellyfin.Plugin.DiscoveryRows.Services;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.DiscoveryRows.Tasks;

public sealed class RefreshTask(LibrarySnapshotService index, HssBridge hss, RecommendationService recommendations) : IScheduledTask
{
    public string Name => "Discovery Rows: rebuild local index";
    public string Key => "DiscoveryRowsRefresh";
    public string Description => "Refresh local discovery data and retry Home Screen Sections registration.";
    public string Category => "Discovery Rows";
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [new() { Type = TaskTriggerInfoType.IntervalTrigger, IntervalTicks = TimeSpan.FromHours(6).Ticks }];
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    { hss.Register(); await index.RefreshAsync(cancellationToken).ConfigureAwait(false); recommendations.Invalidate(); progress.Report(100); }
}

public sealed class EnrichmentTask(TmdbEnrichmentService enrichment) : IScheduledTask
{
    public string Name => "Discovery Rows: optional TMDB enrichment";
    public string Key => "DiscoveryRowsEnrichment";
    public string Description => "Update supported ratings, keywords and episode manifests for local provider IDs. Requires an administrator-supplied token.";
    public string Category => "Discovery Rows";
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [new() { Type = TaskTriggerInfoType.IntervalTrigger, IntervalTicks = TimeSpan.FromHours(6).Ticks }];
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken) => enrichment.RunAsync(progress, cancellationToken);
}
