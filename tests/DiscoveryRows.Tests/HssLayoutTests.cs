using System.Text.Json.Nodes;
using DiscoveryRows.Core;
using Jellyfin.Plugin.DiscoveryRows.Services;
using Xunit;

namespace DiscoveryRows.Tests;

public sealed class HssLayoutTests
{
    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();
    private static JsonObject Row(JsonObject config, string id) => config["SectionSettings"]!.AsArray().OfType<JsonObject>().Single(s => s["SectionId"]!.GetValue<string>() == id);

    [Fact] public void SharedSetupPreservesOtherSettingsStylesAndOriginalInput()
    {
        var config = Parse("""{"Enabled":false,"AllowUserOverride":true,"Sonarr":{"ApiKey":"fixture-only","Url":"https://fixture.invalid"},"SectionSettings":[{"SectionId":"ContinueWatching","Enabled":true,"OrderIndex":2,"AllowUserOverride":true},{"SectionId":"DiscoveryRowsUnder90Movies","Enabled":false,"OrderIndex":20,"ViewMode":1,"HideWatchedItems":true,"LowerLimit":3,"UpperLimit":4,"PluginConfigurations":[{"Key":"custom","Value":"kept"}]}]}""");
        var before = config.ToJsonString();
        var result = HssLayoutEditor.Prepare(config, false);
        Assert.Equal(before, config.ToJsonString());
        Assert.False(result["Enabled"]!.GetValue<bool>());
        Assert.True(JsonNode.DeepEquals(config["Sonarr"], result["Sonarr"]));
        Assert.True(JsonNode.DeepEquals(Row(config, "ContinueWatching"), Row(result, "ContinueWatching")));
        var ours = Row(result, "DiscoveryRowsUnder90Movies");
        Assert.Equal(20, ours["OrderIndex"]!.GetValue<int>());
        Assert.Equal(1, ours["ViewMode"]!.GetValue<int>());
        Assert.True(ours["HideWatchedItems"]!.GetValue<bool>());
        Assert.True(JsonNode.DeepEquals(Row(config, "DiscoveryRowsUnder90Movies")["PluginConfigurations"], ours["PluginConfigurations"]));
        Assert.Equal(1, ours["LowerLimit"]!.GetValue<int>());
        Assert.Equal(1, ours["UpperLimit"]!.GetValue<int>());
        Assert.True(HssLayoutEditor.Inspect(result).Simplified);
    }

    [Fact] public void MissingAndPlaceholderEntriesGetUniqueOrdersAndRepeatIsStable()
    {
        var config = Parse("""{"SectionSettings":[{"SectionId":"Recent","Enabled":true,"OrderIndex":10},{"SectionId":"Disabled","Enabled":false,"OrderIndex":11},{"SectionId":"Unused","Enabled":false,"OrderIndex":999},{"SectionId":"DiscoveryRowsUnder90Movies","OrderIndex":999}]}""");
        var result = HssLayoutEditor.Prepare(config, false);
        var orders = RowCatalogue.All.Select(r => Row(result, r.SectionId)["OrderIndex"]!.GetValue<int>()).ToArray();
        Assert.Equal(38, orders.Distinct().Count());
        Assert.All(orders, n => Assert.True(n > 11 && n < 999));
        Assert.True(JsonNode.DeepEquals(result, HssLayoutEditor.Prepare(result, false)));
    }

    [Fact] public void PersonalSelectionIsOptionalAndDoesNotChangeOrder()
    {
        var shared = HssLayoutEditor.Prepare(new JsonObject(), false);
        var personal = HssLayoutEditor.Prepare(shared, true);
        var status = HssLayoutEditor.Inspect(personal);
        Assert.Equal(38, status.EnabledRows);
        Assert.Equal(0, status.LockedRows);
        Assert.False(status.Simplified);
        foreach (var r in RowCatalogue.All)
            Assert.Equal(Row(shared, r.SectionId)["OrderIndex"]!.GetValue<int>(), Row(personal, r.SectionId)["OrderIndex"]!.GetValue<int>());
    }

    [Fact] public void OnlyExactOwnedIdsAreChangedAndOwnedDuplicatesAreCollapsed()
    {
        var config = Parse("""{"SectionSettings":[{"SectionId":"DiscoveryRowsAnotherPlugin","Enabled":false,"OrderIndex":7},{"SectionId":"DiscoveryRowsUnder90Movies","OrderIndex":5,"ViewMode":2},{"SectionId":"DiscoveryRowsUnder90Movies","OrderIndex":6,"ViewMode":3}]}""");
        var result = HssLayoutEditor.Prepare(config, false);
        Assert.Equal(39, result["SectionSettings"]!.AsArray().Count);
        Assert.True(JsonNode.DeepEquals(Row(config, "DiscoveryRowsAnotherPlugin"), Row(result, "DiscoveryRowsAnotherPlugin")));
        Assert.Equal(2, Row(result, "DiscoveryRowsUnder90Movies")["ViewMode"]!.GetValue<int>());
    }

    [Fact] public void OverflowDoesNotMutateTheOriginalConfiguration()
    {
        var config = Parse("""{"SectionSettings":[{"SectionId":"Recent","Enabled":true,"OrderIndex":2147483647}]}""");
        var before = config.ToJsonString();
        Assert.Throws<OverflowException>(() => HssLayoutEditor.Prepare(config, false));
        Assert.Equal(before, config.ToJsonString());
    }
}
