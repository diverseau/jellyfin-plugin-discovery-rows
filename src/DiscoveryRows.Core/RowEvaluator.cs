using System.Security.Cryptography;
using System.Text;

namespace DiscoveryRows.Core;

public static class RowEvaluator
{
    public static bool Fits(long?[] runtimes, double minutes, bool strict = false) => runtimes.Length > 0 && runtimes.All(r => r is > 0 && (strict ? r.Value < TimeSpan.FromMinutes(minutes).Ticks : r.Value <= TimeSpan.FromMinutes(minutes).Ticks));

    public static FacetAssertion? Facet(WorkEvidence? e, string name, double confidence) => e?.Facets.FirstOrDefault(f => f.Name == name && f.Positive && f.Reviewed && f.Confidence >= confidence);

    public static double? AdjustedRating(RatingEvidence? rating, RowSettings settings, DateTimeOffset now)
    {
        if (rating?.Votes is not { } votes || votes < settings.MinimumVotes || !double.IsFinite(rating.Value) || rating.Scale <= 0 || rating.Value < 0 || rating.Value > rating.Scale || rating.FetchedAt < now.AddDays(-90) || rating.FetchedAt > now.AddDays(1)) return null;
        var normalized = rating.Value / rating.Scale * 10;
        return (votes * normalized + settings.RatingStrength * settings.RatingBaseline) / (votes + settings.RatingStrength);
    }

    public static Evaluation Evaluate(RowDefinition row, RowSettings settings, MediaCandidate item, WorkEvidence? evidence, RankingContext context)
    {
        Evaluation No(string reason) => new(false, reason);
        if (!settings.Enabled) return No("Row disabled");
        if (!item.Available) return No("No accessible local media");
        if (row.Kind is { } kind && item.Kind != kind) return No("Wrong media type");
        if (evidence?.Excluded == true || context.HiddenWorks.Contains(item.WorkKey)) return No("Explicit exclusion");
        if (settings.ExcludeStarted && item.Started) return No("Already watched or started");
        if (row.Selector is SelectorKind.Hidden or SelectorKind.Gems && item.Started) return No("Discovery requires an unwatched, unstarted work");
        if (row.Genre is { } genre && !item.Genres.Contains(genre, StringComparer.OrdinalIgnoreCase)) return No($"Missing {genre} genre");
        if (item.Kind == MediaKind.Series && !HasStart(item)) return No("Missing, inaccessible or ambiguous regular starting episode");
        var adjusted = AdjustedRating(evidence?.Rating, settings, context.Now);
        var fit = .8;
        string? matchSource = null, matchReason = null;
        MetadataMatch? Resolve(string name, double confidence)
        {
            var review = evidence?.Facets.FirstOrDefault(f => f.Name == name && f.Reviewed);
            if (review is not null)
            {
                if (!review.Positive || review.Confidence < confidence) return null;
                matchSource ??= "reviewed_label";
                return new("Reviewed label: " + name, review.Confidence);
            }
            var automatic = context.UseAutomaticMetadata ? AutomaticMetadataMatcher.Match(name, item, evidence) : null;
            if (automatic is not null) matchSource = "automatic_metadata";
            return automatic;
        }
        string Missing(string name) => evidence?.Facets.Any(f => f.Name == name && f.Reviewed && !f.Positive) == true
            ? "Excluded by reviewed " + name + " override"
            : context.UseAutomaticMetadata && AutomaticMetadataMatcher.Supports(name) ? "No automatic metadata match for " + name + " (genres, tags or catalogue keywords)"
            : "Missing reviewed " + name + " evidence";
        long? runtime = null;
        switch (row.Selector)
        {
            case SelectorKind.Runtime:
                if (!Fits(item.Runtimes, row.Minutes!.Value, row.Strict)) return No("Missing runtime or a selectable edition exceeds the limit");
                runtime = item.Runtimes.Max();
                break;
            case SelectorKind.ShortEpisodes:
                var regular = item.Episodes.Where(e => e.Season > 0).ToArray();
                if (regular.Length == 0 || regular.Any(e => !e.Accessible || !Fits(e.Runtimes, row.Minutes!.Value))) return No("A regular episode is inaccessible, unknown or too long");
                break;
            case SelectorKind.Facet:
            case SelectorKind.Theme:
                var facet = Resolve(row.Facet!, row.Confidence);
                if (facet is null) return No(Missing(row.Facet!));
                fit = facet.Strength;
                matchReason = facet.Reason;
                if (row.Key == "casual")
                {
                    var episodic = Resolve("episodic", .75);
                    if (episodic is null) return No(Missing("episodic"));
                    matchReason += "; " + episodic.Reason;
                }
                if (row.Key == "crowd" && (adjusted is null || adjusted < settings.QualityMinimum)) return No("Missing supported audience approval");
                break;
            case SelectorKind.Quality:
            case SelectorKind.Gems:
                if (adjusted is null || adjusted < settings.QualityMinimum) return No("Missing supported rating/votes or below quality floor");
                if (row.Selector == SelectorKind.Gems)
                {
                    if (context.ExposureCutoff is not { } cutoff || evidence?.Popularity is not { } exposure || evidence.PopularityProvider != "tmdb" || evidence.ProviderFetchedAt is null || evidence.ProviderFetchedAt < context.Now.AddDays(-90) || exposure > cutoff) return No("Missing or excessive relative exposure");
                    if (context.RecentlyDelivered.Contains(item.WorkKey)) return No("Recently recommended in a prior time bucket");
                }
                break;
            case SelectorKind.Hidden:
                if (context.RecentlyDelivered.Contains(item.WorkKey)) return No("Recently recommended in a prior time bucket");
                break;
            case SelectorKind.Award:
                if (evidence is null || !evidence.Awards.Any(a => a.WorkKey == item.WorkKey && a.Body == row.AwardBody && (!row.WinnerOnly || a.Outcome == "winner") && (settings.AwardYears.Length == 0 || settings.AwardYears.Contains(a.CeremonyYear)) && (settings.AwardCategories.Length == 0 || settings.AwardCategories.Contains(a.Category, StringComparer.OrdinalIgnoreCase)))) return No("Missing matching work-linked award");
                break;
            case SelectorKind.CompleteStory:
            case SelectorKind.CompleteSeries:
            case SelectorKind.FirstSeason:
                if (row.Selector == SelectorKind.CompleteStory)
                {
                    var story = Resolve("complete_limited_story", .85);
                    if (story is null) return No(Missing("complete_limited_story"));
                    matchReason = story.Reason;
                }
                if (row.Selector == SelectorKind.FirstSeason && (item.FirstSeasonId is null || Facet(evidence, row.Facet!, row.Confidence) is null)) return No("Missing first-season card or reviewed first-season evidence");
                var completeness = CompleteRuntime(item, evidence?.Manifest, context.Now, row.Selector == SelectorKind.FirstSeason);
                if (!completeness.Complete) return No(completeness.Reason);
                runtime = completeness.Ticks;
                if (row.Minutes is { } limit && runtime > TimeSpan.FromMinutes(limit).Ticks) return No("Complete story exceeds time budget");
                break;
        }
        var taste = item.Genres.Select(g => context.GenreTaste.GetValueOrDefault(g, .5)).DefaultIfEmpty(.5).Average();
        var quality = adjusted is { } q ? q / 10 : .5;
        var freshness = Math.Clamp(1 - (context.Now - item.AddedAt).TotalDays / 180, 0, 1);
        var discovery = context.RecentlyDelivered.Contains(item.WorkKey) ? 0 : 1;
        var score = row.Selector switch
        {
            SelectorKind.Quality or SelectorKind.Award => .25 * taste + .65 * quality + .1 * fit,
            SelectorKind.Gems => .35 * taste + .3 * quality + .1 * fit + .25 * discovery,
            SelectorKind.Hidden => .4 * taste + .15 * quality + .15 * fit + .05 * freshness + .25 * discovery,
            SelectorKind.Facet or SelectorKind.Theme => .3 * taste + .1 * quality + .5 * fit + .1 * freshness,
            _ => .35 * taste + .15 * quality + .4 * fit + .1 * freshness
        };
        return new(true, matchReason ?? "All factual and access gates passed", score, taste, quality, fit, runtime, matchSource);
    }

    public static bool HasStart(MediaCandidate item) => item.EpisodeOrder == "aired" && item.Episodes.Any(e => e.Season == 1 && e.Number == 1 && e.Accessible && e.Runtimes.Length > 0 && e.Runtimes.All(r => r is > 0));

    public static (bool Complete, long Ticks, string Reason) CompleteRuntime(MediaCandidate item, EpisodeManifest? manifest, DateTimeOffset now, bool firstSeason = false)
    {
        if (manifest is null || manifest.WorkKey != item.WorkKey || !manifest.Complete || (!firstSeason && !manifest.Finished) || manifest.Order != "aired" || item.EpisodeOrder != "aired" || manifest.FetchedAt < now.AddDays(-30) || manifest.FetchedAt > now.AddDays(1)) return (false, 0, "Missing fresh complete manifest or finished status");
        var required = manifest.Episodes.Where(e => e.Required && (!firstSeason || e.Season == 1)).ToArray();
        if (required.Length == 0 || required.Any(e => e.AirDate is null || e.AirDate > now)) return (false, 0, "Manifest has unknown or unaired required episodes");
        var files = new Dictionary<Guid, long>();
        foreach (var e in required)
        {
            var local = item.Episodes.Where(l => l.Accessible && l.Season == e.Season && l.Number <= e.Number && (l.EndNumber > 0 ? l.EndNumber : l.Number) >= e.Number && l.Runtimes.Length > 0 && l.Runtimes.All(r => r is > 0) && (l.Number != l.EndNumber && l.EndNumber > l.Number || e.ProviderId is null || l.ProviderId is null || e.ProviderId == l.ProviderId)).OrderBy(l => l.Id).FirstOrDefault();
            if (local is null) return (false, 0, $"Missing or inaccessible episode {e.Season}:{e.Number}");
            // A double episode is one file. Conservative maximum preserves alternate-edition promises.
            files[local.Id] = local.Runtimes.Max()!.Value;
        }
        try { return (true, checked(files.Values.Aggregate(0L, (sum, ticks) => checked(sum + ticks))), "All required episodes accessible"); }
        catch (OverflowException) { return (false, 0, "Invalid total duration"); }
    }

    public static RankedCandidate[] Rank(RowDefinition row, RowSettings settings, IEnumerable<MediaCandidate> candidates, IReadOnlyDictionary<string, WorkEvidence> evidence, RankingContext context) => candidates
        .Select(i => new RankedCandidate(i, Evaluate(row, settings, i, evidence.GetValueOrDefault(i.WorkKey), context)))
        .Where(r => r.Explanation.Eligible)
        .OrderByDescending(r => r.Explanation.Score)
        .ThenBy(r => TieBreak(context.UserId, row.Key, r.Item.WorkKey, context.Bucket))
        .GroupBy(r => r.Item.WorkKey, StringComparer.Ordinal).Select(g => g.First()).ToArray();

    public static ulong TieBreak(Guid user, string row, string work, string bucket) => System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes($"{user:N}|{row}|{work}|{bucket}")));
}
