using DiscoveryRows.Core;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.DiscoveryRows.Configuration;

public sealed class GenreAlias
{
    public string Source { get; set; } = "";
    public string Canonical { get; set; } = "";
}

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public bool Enabled { get; set; } = true;
    public bool CoordinateRows { get; set; } = true;
    public bool EnableTmdb { get; set; }
    public bool UseAutomaticMetadata { get; set; } = true;
    public int CacheMinutes { get; set; } = 10;
    public int MaximumCachedUsers { get; set; } = 64;
    public int EnrichmentBatchSize { get; set; } = 100;
    public RowSettings[] Rows { get; set; } = RowCatalogue.All.Select(RowCatalogue.Defaults).ToArray();
    public GenreAlias[] GenreAliases { get; set; } = [];

    public RowSettings GetRow(RowDefinition row) => Rows.FirstOrDefault(s => s.Key == row.Key) ?? RowCatalogue.Defaults(row);

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (CacheMinutes is < 1 or > 60 || MaximumCachedUsers is < 1 or > 256 || EnrichmentBatchSize is < 1 or > 1000) errors.Add("Cache/batch settings are out of range.");
        if (Rows is null || Rows.Length > RowCatalogue.All.Count || GenreAliases is null || GenreAliases.Length > 100) return ["Invalid rows or genre aliases."];
        var keys = new HashSet<string>();
        foreach (var s in Rows)
        {
            if (s is null || RowCatalogue.Find(s.Key) is null || !keys.Add(s.Key)) { errors.Add("Unknown or duplicate row."); continue; }
            if (s.Limit is < 1 or > 40 || s.Minimum < 1 || s.Minimum > s.Limit || s.FranchiseCap is < 1 or > 40 || s.MinimumVotes is < 1 or > 10000000 || !double.IsFinite(s.QualityMinimum) || s.QualityMinimum is < 0 or > 10 || !double.IsFinite(s.RatingBaseline) || s.RatingBaseline is < 0 or > 10 || !double.IsFinite(s.RatingStrength) || s.RatingStrength is < 1 or > 1000000 || s.AwardYears is null || s.AwardCategories is null || s.AwardYears.Length > 200 || s.AwardCategories.Length > 100 || s.AwardYears.Any(y => y is < 1900 or > 2200) || s.AwardCategories.Any(c => string.IsNullOrWhiteSpace(c) || c.Length > 200)) errors.Add($"Invalid settings for {s.Key}.");
        }
        if (GenreAliases.Any(a => a is null || string.IsNullOrWhiteSpace(a.Source) || a.Source.Length > 100 || a.Canonical is not ("Comedy" or "Thriller" or "Documentary" or "Horror" or "Romance" or "Family"))) errors.Add("Aliases must map to a supported canonical genre.");
        return errors;
    }
}
