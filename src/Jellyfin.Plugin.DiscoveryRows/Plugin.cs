using Jellyfin.Plugin.DiscoveryRows.Configuration;
using Jellyfin.Plugin.DiscoveryRows.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.DiscoveryRows;

public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public const string PluginId = "b87d3d47-7a2d-43c6-89d2-592885b90d3e";
    private readonly RecommendationService _recommendations;
    public static Plugin? Instance { get; private set; }

    public Plugin(IApplicationPaths paths, IXmlSerializer serializer, RecommendationService recommendations)
        : base(paths, serializer)
    {
        _recommendations = recommendations;
        Instance = this;
    }

    public override string Name => "Discovery Rows";
    public override Guid Id => Guid.Parse(PluginId);
    public override string Description => "Mood, time, recognition and personal discovery shelves for Home Screen Sections.";
    public override void UpdateConfiguration(BasePluginConfiguration configuration)
    {
        if (configuration is not PluginConfiguration config) throw new ArgumentException("Invalid plugin configuration.", nameof(configuration));
        var errors = config.Validate();
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors), nameof(configuration));
        base.UpdateConfiguration(configuration);
        _recommendations.Invalidate();
    }

    public IEnumerable<PluginPageInfo> GetPages() =>
    [
        new() { Name = "discoveryrows", EmbeddedResourcePath = "Jellyfin.Plugin.DiscoveryRows.Configuration.configPage.html" },
        new() { Name = "discoveryrows-preferences", EmbeddedResourcePath = "Jellyfin.Plugin.DiscoveryRows.Configuration.preferences.html" }
    ];
}
