using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using DiscoveryRows.Core;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DiscoveryRows.Services;

public sealed class HssBridge
{
    private readonly IServiceProvider _services;
    private readonly ILogger<HssBridge> _logger;
    private readonly object _gate = new();
    private readonly HashSet<string> _registered = new(StringComparer.Ordinal);
    private Assembly? _assembly;
    public string Status { get; private set; } = "Waiting for Home Screen Sections";
    public bool LabelsReady { get; private set; }
    public bool LayoutAvailable { get; private set; }
    public bool PreferencesPageReady { get; private set; }
    public int RegisteredCount { get { lock (_gate) return _registered.Count; } }

    public HssBridge(IServiceProvider services, ILogger<HssBridge> logger) { _services = services; _logger = logger; }

    public bool Register()
    {
        lock (_gate)
        {
            try
            {
                _assembly ??= AssemblyLoadContext.All.SelectMany(c => c.Assemblies).FirstOrDefault(a => a.GetName().Name == "Jellyfin.Plugin.HomeScreenSections");
                if (_assembly is null) { Status = "Home Screen Sections is not installed"; return false; }
                var plugin = _assembly.GetType("Jellyfin.Plugin.HomeScreenSections.HomeScreenSectionsPlugin")?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (plugin is null) { Status = "Waiting for Home Screen Sections initialization"; return false; }
                var register = _assembly.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface")?.GetMethod("RegisterSection", BindingFlags.Public | BindingFlags.Static);
                if (register is null || register.GetParameters().Length != 1) throw new InvalidOperationException("Unsupported HSS registration contract.");
                foreach (var row in RowCatalogue.All)
                {
                    if (_registered.Contains(row.SectionId)) continue;
                    var method = "GetRow_" + row.Key;
                    if (typeof(HssResultsHandler).GetMethod(method) is null) throw new InvalidOperationException("Missing result wrapper.");
                    var payload = JsonSerializer.Serialize(new { id = row.SectionId, displayText = row.Title, limit = 1,
                        resultsAssembly = typeof(HssResultsHandler).Assembly.FullName, resultsClass = typeof(HssResultsHandler).FullName,
                        resultsMethod = method });
                    register.Invoke(null, [ParseJObject(register.GetParameters()[0].ParameterType, payload)]);
                    _registered.Add(row.SectionId);
                }
                if (!LabelsReady) SeedLabels();
                if (!PreferencesPageReady) RegisterPreferencesPage();
                Status = LabelsReady ? "Registered; HSS settings must be saved to enable shelves" : "Registered; HSS settings labels need compatibility attention";
                return true;
            }
            catch (Exception ex) when (ex is TargetInvocationException or InvalidOperationException or ArgumentException or TypeLoadException)
            { Status = "HSS registration/label compatibility failed; inspect Jellyfin logs"; _logger.LogWarning(ex, "Discovery Rows HSS registration failed"); return false; }
        }
    }

    public static object ParseJObject(Type expectedType, string json) => expectedType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, [typeof(string)])?.Invoke(null, [json]) ?? throw new InvalidOperationException("Expected HSS JObject.Parse(string) capability.");

    private void SeedLabels()
    {
        var type = _assembly!.GetType("Jellyfin.Plugin.HomeScreenSections.Library.ITranslationManager");
        var service = type is null ? null : _services.GetService(type);
        var update = type?.GetMethod("UpdateTranslationPack");
        if (service is null || update is null) return;
        var json = JsonSerializer.Serialize(RowCatalogue.All.ToDictionary(r => r.SectionId, r => r.Title));
        var languages = _assembly.GetManifestResourceNames().Where(n => n.Contains("._Localization.", StringComparison.Ordinal) && n.EndsWith(".json", StringComparison.Ordinal)).Select(n => n[..^5].Split('.').Last()).Append("en").Distinct(StringComparer.Ordinal);
        foreach (var language in languages) update.Invoke(service, [language, ParseJObject(update.GetParameters()[1].ParameterType, json)]);
        LabelsReady = true;
    }

    private void RegisterPreferencesPage()
    {
        var assembly = AssemblyLoadContext.All.SelectMany(c => c.Assemblies).FirstOrDefault(a => a.GetName().Name == "Jellyfin.Plugin.PluginPages");
        var register = assembly?.GetType("Jellyfin.Plugin.PluginPages.PluginInterface")?.GetMethod("RegisterPage", BindingFlags.Public | BindingFlags.Static);
        if (register is null) return;
        var json = JsonSerializer.Serialize(new { Id = "Jellyfin.Plugin.DiscoveryRows.Preferences", Url = "/web/ConfigurationPage?name=discoveryrows-preferences", DisplayText = "Discovery preferences", Icon = "tune" });
        register.Invoke(null, [ParseJObject(register.GetParameters()[0].ParameterType, json)]);
        PreferencesPageReady = true;
    }

    public string[]? EffectiveRows(Guid user)
    {
        try
        {
            var type = _assembly?.GetType("Jellyfin.Plugin.HomeScreenSections.Library.IHomeScreenManager");
            var manager = type is null ? null : _services.GetService(type);
            var settings = type?.GetMethod("GetUserSettings")?.Invoke(manager, [user]);
            var enabled = settings?.GetType().GetProperty("EnabledSections")?.GetValue(settings) as IEnumerable;
            if (enabled is null) { LayoutAvailable = false; return null; }
            var ids = enabled.Cast<object>().OfType<string>().ToHashSet(StringComparer.Ordinal);
            LayoutAvailable = true;
            return RowCatalogue.All.Where(r => ids.Contains(r.SectionId)).Select(r => r.Key).ToArray();
        }
        catch (Exception ex) when (ex is TargetInvocationException or ArgumentException or InvalidOperationException)
        { LayoutAvailable = false; _logger.LogDebug(ex, "HSS effective layout read unavailable; independent rows remain available"); return null; }
    }
}
