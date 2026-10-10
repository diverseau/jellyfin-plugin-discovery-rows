using Jellyfin.Plugin.DiscoveryRows.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.DiscoveryRows;

public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection services, IServerApplicationHost applicationHost)
    {
        services.AddHttpContextAccessor();
        services.AddHttpClient("DiscoveryRowsTmdb", client => { client.BaseAddress = new Uri("https://api.themoviedb.org/3/"); client.Timeout = TimeSpan.FromSeconds(15); });
        services.AddSingleton<EvidenceStore>(); services.AddSingleton<UserHistoryStore>(); services.AddSingleton<LibrarySnapshotService>();
        services.AddSingleton<HssBridge>(); services.AddSingleton<RecommendationService>(); services.AddSingleton<TmdbEnrichmentService>();
        services.AddHostedService<BackgroundWorker>();
    }
}
