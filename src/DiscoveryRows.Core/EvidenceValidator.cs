using System.Text.RegularExpressions;

namespace DiscoveryRows.Core;

public static partial class EvidenceValidator
{
    [GeneratedRegex(@"^(movie|series):(tmdb:[1-9][0-9]*|imdb:tt[0-9]+|jellyfin:[a-f0-9]{32})$")]
    private static partial Regex KeyPattern();
    [GeneratedRegex(@"^[a-z0-9_]{1,64}$")]
    private static partial Regex FacetPattern();
    public static bool IsWorkKey(string? key) => key is not null && KeyPattern().IsMatch(key);

    public static IReadOnlyList<string> Validate(EvidenceDocument document, DateTimeOffset now)
    {
        var errors = new List<string>();
        if (document.SchemaVersion != 1) errors.Add("Only schemaVersion 1 is supported.");
        if (document.Items is null || document.Items.Length > 25000) return ["Items must be an array of at most 25,000 records."];
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var w in document.Items)
        {
            if (w is null) { errors.Add("Null evidence record."); continue; }
            var key = w.WorkKey;
            if (!IsWorkKey(key) || !keys.Add(key)) errors.Add($"Invalid or duplicate work key: {key}");
            if (w.Facets is null || w.Awards is null || w.Keywords is null) { errors.Add($"{key}: arrays must not be null."); continue; }
            if (w.Facets.Length > 100 || w.Awards.Length > 500 || w.Keywords.Length > 500) errors.Add($"{key}: too many evidence fields.");
            var facets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var f in w.Facets)
            {
                if (f is null) { errors.Add($"{key}: null facet."); continue; }
                if (f.Name is null || !FacetPattern().IsMatch(f.Name) || !facets.Add(f.Name)) errors.Add($"{key}: invalid/duplicate facet {f.Name}.");
                if (!double.IsFinite(f.Confidence) || f.Confidence < 0 || f.Confidence > 1) errors.Add($"{key}: facet confidence must be 0–1.");
                if (string.IsNullOrWhiteSpace(f.Source) || f.Source.Length > 100 || (f.Reviewed && !ValidDate(f.ReviewedAt, now))) errors.Add($"{key}: reviewed facets need a source and review time.");
                if (f.EvidenceUrl is not null && !IsUrl(f.EvidenceUrl)) errors.Add($"{key}: invalid facet evidence URL.");
            }
            if (w.Rating is { } r && (string.IsNullOrWhiteSpace(r.Provider) || r.Provider.Length > 64 || !double.IsFinite(r.Scale) || r.Scale <= 0 || r.Scale > 100 || !double.IsFinite(r.Value) || r.Value < 0 || r.Value > r.Scale || r.Votes is < 0 || !ValidDate(r.FetchedAt, now))) errors.Add($"{key}: invalid provider rating or timestamp.");
            if (w.Popularity is { } p && (!double.IsFinite(p) || p < 0 || string.IsNullOrWhiteSpace(w.PopularityProvider) || !FacetPattern().IsMatch(w.PopularityProvider) || w.ProviderFetchedAt is null || !ValidDate(w.ProviderFetchedAt.Value, now))) errors.Add($"{key}: popularity needs an attributed provider, nonnegative value and provider timestamp.");
            if (w.Keywords.Any(k => string.IsNullOrWhiteSpace(k) || k.Length > 200)) errors.Add($"{key}: invalid keyword.");
            var awards = new HashSet<string>();
            foreach (var a in w.Awards)
            {
                if (a is null) { errors.Add($"{key}: null award."); continue; }
                if (a.WorkKey != key || a.Body is null || !FacetPattern().IsMatch(a.Body) || a.CeremonyYear < 1900 || a.CeremonyYear > now.Year + 1 || string.IsNullOrWhiteSpace(a.Category) || a.Category.Length > 200 || a.Outcome is not ("nominee" or "winner") || !IsUrl(a.EvidenceUrl) || string.IsNullOrWhiteSpace(a.Contribution)) errors.Add($"{key}: awards must identify this work, category, outcome, ceremony and evidence URL.");
                if (!awards.Add($"{a.Body}:{a.CeremonyYear}:{a.Category}:{a.Contribution}")) errors.Add($"{key}: duplicate/conflicting award record.");
            }
            if (w.Manifest is { } m)
            {
                if (!key.StartsWith("series:", StringComparison.Ordinal) || m.WorkKey != key || m.Order != "aired" || string.IsNullOrWhiteSpace(m.Source) || !ValidDate(m.FetchedAt, now) || m.Episodes is null || m.Episodes.Length == 0 || m.Episodes.Length > 20000) { errors.Add($"{key}: invalid aired-order series manifest."); continue; }
                var episodeKeys = new HashSet<(int, int)>();
                foreach (var e in m.Episodes)
                {
                    if (e is null) { errors.Add($"{key}: null manifest episode."); continue; }
                    if (e.Season < 0 || e.Number < 1 || !episodeKeys.Add((e.Season, e.Number))) errors.Add($"{key}: invalid/duplicate episode coordinates.");
                }
            }
            if (errors.Count > 100) break;
        }
        return errors;
    }

    private static bool ValidDate(DateTimeOffset date, DateTimeOffset now) => date.Year >= 2000 && date <= now.AddDays(1);
    private static bool IsUrl(string? value) => value is not null && value.Length <= 2000 && Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http";
}
