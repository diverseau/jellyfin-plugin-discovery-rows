using System.Text.Json.Nodes;
using DiscoveryRows.Core;

namespace Jellyfin.Plugin.DiscoveryRows.Services;

public sealed record HomepageSetupStatus(bool Available, int EnabledRows, int LockedRows, int TotalRows)
{
    public bool Simplified => Available && EnabledRows == TotalRows && LockedRows == TotalRows;
}

/// <summary>Prepare this plugin's HSS entries without changing other sections or global settings.</summary>
public static class HssLayoutEditor
{
    public static HomepageSetupStatus Inspect(JsonObject config)
    {
        var sections = (config["SectionSettings"] as JsonArray)?.OfType<JsonObject>() ?? [];
        var byId = sections.GroupBy(Id).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var enabled = RowCatalogue.All.Select(r => byId.GetValueOrDefault(r.SectionId))
            .Where(s => s?["Enabled"]?.GetValue<bool>() == true).ToArray();
        return new(true, enabled.Length, enabled.Count(s => s!["AllowUserOverride"]?.GetValue<bool>() == false), RowCatalogue.All.Count);
    }

    public static JsonObject Prepare(JsonObject original, bool allowUserSelection)
    {
        var config = (JsonObject)original.DeepClone();
        var sections = config["SectionSettings"] as JsonArray ?? new JsonArray();
        var ownedIds = RowCatalogue.All.Select(r => r.SectionId).ToHashSet(StringComparer.Ordinal);
        var existing = sections.OfType<JsonObject>().ToArray();
        var owned = existing.Where(s => ownedIds.Contains(Id(s))).GroupBy(Id).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var reserved = existing.Select(Order).ToHashSet();
        var lastOrder = existing.Where(s => ownedIds.Contains(Id(s)) ? Order(s) != 999 : s["Enabled"]?.GetValue<bool>() == true)
            .Select(Order).DefaultIfEmpty(0).Max();
        var prepared = new JsonArray();
        foreach (var section in sections.Where(s => s is not JsonObject o || !ownedIds.Contains(Id(o))))
            prepared.Add(section?.DeepClone());
        foreach (var row in RowCatalogue.All)
        {
            var prior = owned.GetValueOrDefault(row.SectionId);
            var section = prior is null ? new JsonObject {
                ["SectionId"] = row.SectionId, ["ViewMode"] = 0,
                ["HideWatchedItems"] = false, ["PluginConfigurations"] = new JsonArray()
            } : (JsonObject)prior.DeepClone();
            if (prior is null || Order(section) == 999)
            {
                do { lastOrder = checked(lastOrder + 1); } while (reserved.Contains(lastOrder));
                section["OrderIndex"] = lastOrder;
                reserved.Add(lastOrder);
            }
            section["Enabled"] = true;
            section["AllowUserOverride"] = allowUserSelection;
            section["LowerLimit"] = 1;
            section["UpperLimit"] = 1;
            prepared.Add(section);
        }
        config["SectionSettings"] = prepared;
        return config;
    }

    private static string Id(JsonObject section) => section["SectionId"]?.GetValue<string>() ?? "";
    private static int Order(JsonObject section) => section["OrderIndex"]?.GetValue<int>() ?? 999;
}
