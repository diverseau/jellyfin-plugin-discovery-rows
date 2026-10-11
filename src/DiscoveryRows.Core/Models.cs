namespace DiscoveryRows.Core;

public enum MediaKind { Movie, Series }
public enum SelectorKind { Runtime, ShortEpisodes, Facet, Quality, Hidden, Gems, Award, CompleteStory, CompleteSeries, FirstSeason, Theme }

public sealed record LocalEpisode
{
    public Guid Id { get; init; }
    public int Season { get; init; }
    public int Number { get; init; }
    public int EndNumber { get; init; }
    public bool Accessible { get; init; }
    public bool Started { get; init; }
    public long?[] Runtimes { get; init; } = [];
    public string? ProviderId { get; init; }
}

public sealed record MediaCandidate
{
    public Guid Id { get; init; }
    public string WorkKey { get; init; } = "";
    public string Name { get; init; } = "";
    public MediaKind Kind { get; init; }
    public bool Available { get; init; }
    public bool Started { get; init; }
    public bool Favourite { get; init; }
    public bool? Liked { get; init; }
    public DateTimeOffset? LastPlayedAt { get; init; }
    public DateTimeOffset AddedAt { get; init; }
    public string[] Genres { get; init; } = [];
    public string[] Tags { get; init; } = [];
    public int? ProductionYear { get; init; }
    public string? OfficialRating { get; init; }
    public long?[] Runtimes { get; init; } = [];
    public LocalEpisode[] Episodes { get; init; } = [];
    public string EpisodeOrder { get; init; } = "aired";
    public string? FranchiseKey { get; init; }
    public Guid? FirstSeasonId { get; init; }
}

public sealed record RatingEvidence
{
    public string Provider { get; init; } = "tmdb";
    public double Value { get; init; }
    public double Scale { get; init; } = 10;
    public long? Votes { get; init; }
    public DateTimeOffset FetchedAt { get; init; }
}

public sealed record FacetAssertion
{
    public string Name { get; init; } = "";
    public bool Positive { get; init; } = true;
    public double Confidence { get; init; }
    public bool Reviewed { get; init; }
    public string Source { get; init; } = "manual_review";
    public string? EvidenceUrl { get; init; }
    public DateTimeOffset ReviewedAt { get; init; }
}

public sealed record AwardRecord
{
    public string WorkKey { get; init; } = "";
    public string Body { get; init; } = "";
    public int CeremonyYear { get; init; }
    public string Category { get; init; } = "";
    public string Outcome { get; init; } = "";
    public string Contribution { get; init; } = "work";
    public string EvidenceUrl { get; init; } = "";
}

public sealed record ManifestEpisode
{
    public int Season { get; init; }
    public int Number { get; init; }
    public string? ProviderId { get; init; }
    public bool Required { get; init; } = true;
    public DateTimeOffset? AirDate { get; init; }
}

public sealed record EpisodeManifest
{
    public string WorkKey { get; init; } = "";
    public string Order { get; init; } = "aired";
    public bool Finished { get; init; }
    public bool Complete { get; init; }
    public string Source { get; init; } = "manual_review";
    public DateTimeOffset FetchedAt { get; init; }
    public ManifestEpisode[] Episodes { get; init; } = [];
}

public sealed record WorkEvidence
{
    public string WorkKey { get; init; } = "";
    public bool Excluded { get; init; }
    public FacetAssertion[] Facets { get; init; } = [];
    public RatingEvidence? Rating { get; init; }
    public double? Popularity { get; init; }
    public string? PopularityProvider { get; init; }
    public DateTimeOffset? ProviderFetchedAt { get; init; }
    public string[] Keywords { get; init; } = [];
    public CatalogueMetadata? Metadata { get; init; }
    public AwardRecord[] Awards { get; init; } = [];
    public EpisodeManifest? Manifest { get; init; }
}

public sealed record CatalogueMetadata
{
    public string Source { get; init; } = "tmdb";
    public string[] Genres { get; init; } = [];
    public string[] Countries { get; init; } = [];
    public int? Year { get; init; }
    public string? SeriesType { get; init; }
    public string? Status { get; init; }
}

public sealed record EvidenceDocument
{
    public int SchemaVersion { get; init; } = 1;
    public WorkEvidence[] Items { get; init; } = [];
}

public sealed record RowDefinition(string Key, string SectionId, string Title, MediaKind? Kind, SelectorKind Selector,
    double? Minutes = null, bool Strict = false, string? Genre = null, string? Facet = null, double Confidence = .75,
    string? AwardBody = null, bool WinnerOnly = false);

public sealed record RowSettings
{
    public string Key { get; init; } = "";
    public bool Enabled { get; init; } = true;
    public int Limit { get; init; } = 16;
    public int Minimum { get; init; } = 6;
    public bool ExcludeStarted { get; init; } = true;
    public int FranchiseCap { get; init; } = 2;
    public double QualityMinimum { get; init; } = 7;
    public long MinimumVotes { get; init; } = 100;
    public double RatingBaseline { get; init; } = 6.5;
    public double RatingStrength { get; init; } = 1000;
    public int[] AwardYears { get; init; } = [];
    public string[] AwardCategories { get; init; } = [];
}

public sealed record RankingContext
{
    public bool UseAutomaticMetadata { get; init; }
    public Guid UserId { get; init; }
    public DateTimeOffset Now { get; init; } = DateTimeOffset.UtcNow;
    public string Bucket => Now.UtcDateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    public IReadOnlyDictionary<string, double> GenreTaste { get; init; } = new Dictionary<string, double>();
    public IReadOnlySet<string> HiddenWorks { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> RecentlyDelivered { get; init; } = new HashSet<string>();
    public double? ExposureCutoff { get; init; }
}

public sealed record Evaluation(bool Eligible, string Reason, double Score = 0, double Taste = .5,
    double Quality = .5, double RowFit = .5, long? RequiredRuntimeTicks = null, string? MatchSource = null);
public sealed record RankedCandidate(MediaCandidate Item, Evaluation Explanation);
