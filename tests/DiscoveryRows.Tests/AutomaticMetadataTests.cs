using System.Text.Json;
using DiscoveryRows.Core;
using Jellyfin.Plugin.DiscoveryRows.Configuration;
using Jellyfin.Plugin.DiscoveryRows.Services;
using Xunit;

namespace DiscoveryRows.Tests;

public sealed class AutomaticMetadataTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly RankingContext Automatic = new() { UseAutomaticMetadata = true, Now = Now };
    private static MediaCandidate Movie(string genres = "Family|Comedy") => new() { Id = Guid.NewGuid(), WorkKey = "movie:tmdb:1", Kind = MediaKind.Movie,
        Available = true, Genres = genres.Split('|', StringSplitOptions.RemoveEmptyEntries), OfficialRating = "PG", ProductionYear = 1985,
        Runtimes = [TimeSpan.FromMinutes(85).Ticks] };
    private static MediaCandidate Series(string genres) => Movie(genres) with { Kind = MediaKind.Series, WorkKey = "series:tmdb:1", Episodes = [new() {
        Id = Guid.NewGuid(), Season = 1, Number = 1, Accessible = true, Runtimes = [TimeSpan.FromMinutes(25).Ticks] }] };
    private static Evaluation Eval(string key, MediaCandidate item, WorkEvidence? evidence = null, bool automatic = true) =>
        RowEvaluator.Evaluate(RowCatalogue.Find(key)!, RowCatalogue.Defaults(RowCatalogue.Find(key)!) with { Enabled = true }, item, evidence, Automatic with { UseAutomaticMetadata = automatic });

    [Theory]
    [InlineData("family", "Family", "")]
    [InlineData("laughmovies", "Comedy", "")]
    [InlineData("feelgood", "Comedy|Romance", "")]
    [InlineData("popcorn", "Action", "")]
    [InlineData("crowd", "Adventure", "")]
    [InlineData("casual", "Comedy", "")]
    [InlineData("chosenfamily", "Drama", "found family")]
    [InlineData("cyberpunk", "Science Fiction", "cyberspace")]
    [InlineData("dystopia", "Science Fiction", "dystopia")]
    [InlineData("food", "Drama", "chef")]
    [InlineData("smalltown", "Comedy", "small town")]
    [InlineData("smalltownscares", "Horror", "small town")]
    [InlineData("scams", "Crime", "con artist")]
    [InlineData("arthorror", "Horror", "psychological horror")]
    [InlineData("gentlereality", "Reality", "baking")]
    [InlineData("witchcraft", "Fantasy", "witchcraft")]
    [InlineData("books", "Drama", "based on novel or book")]
    [InlineData("truestory", "Drama", "based on a true story")]
    [InlineData("nostalgia80", "Drama", "")]
    [InlineData("twistedchristmas", "Horror", "christmas")]
    [InlineData("cringe", "Comedy", "mockumentary")]
    public void MoodsAndEveryThemeCanMatchWithoutReviewedLabels(string key, string genres, string keyword)
    {
        var item = key is "casual" or "gentlereality" ? Series(genres) : Movie(genres);
        var evidence = new WorkEvidence { WorkKey = item.WorkKey, Keywords = keyword.Length == 0 ? [] : [keyword], Metadata = new() { Countries = ["GB"] },
            Rating = new() { Value = 8, Votes = 10000, FetchedAt = Now } };
        var result = Eval(key, item, evidence);
        Assert.True(result.Eligible, result.Reason);
        Assert.Equal("automatic_metadata", result.MatchSource);
        Assert.Contains("Automatic metadata:", result.Reason);
        Assert.Empty(evidence.Facets);
        Assert.False(Eval(key, item, evidence, false).Eligible);
    }

    [Theory] [InlineData("R")] [InlineData("TV-MA")] [InlineData("AU:MA15+")] [InlineData("Rated R")]
    public void KnownMatureRatingsDoNotAutoMatchFamily(string rating) => Assert.False(Eval("family", Movie() with { OfficialRating = rating }).Eligible);
    [Theory] [InlineData("Family|Horror")] [InlineData("Family|Crime")] [InlineData("Family|Thriller")]
    public void DarkGenresDoNotAutoMatchFamily(string genres) => Assert.False(Eval("family", Movie(genres)).Eligible);
    [Fact] public void AnimationAloneDoesNotImplyFamily() => Assert.False(Eval("family", Movie("Animation") with { OfficialRating = null }).Eligible);
    [Fact] public void FamilyMovieNightRejectsSeries() => Assert.False(Eval("family", Series("Family")).Eligible);
    [Fact] public void GenericFamilyKeywordDoesNotImplyFamilyIntent() => Assert.False(Eval("family", Movie("Drama"), new() { Keywords = ["family", "family relationships"] }).Eligible);
    [Fact] public void DarkComedyDoesNotBecomeFeelGood() => Assert.False(Eval("feelgood", Movie("Comedy|Romance"), new() { Keywords = ["black comedy"] }).Eligible);
    [Fact] public void GenericComedyDoesNotBecomeFeelGood() => Assert.False(Eval("feelgood", Movie("Comedy")).Eligible);
    [Fact] public void ExactKeywordMatchingDoesNotMatchMisleadingSubstrings() => Assert.False(Eval("witchcraft", Movie("Drama"), new() { Keywords = ["witch hunt", "sandwich"] }).Eligible);
    [Fact] public void UnknownFacetsAreNotInventedAsCatalogueEvidence() => Assert.Null(AutomaticMetadataMatcher.Match("unknown_label", Movie(), null));
    [Fact] public void ReviewedRejectionBeatsAutomaticMatch()
    {
        var work = new WorkEvidence { Facets = [new() { Name = "family_intent", Positive = false, Reviewed = true, Confidence = 1, ReviewedAt = Now }] };
        var result = Eval("family", Movie(), work);
        Assert.False(result.Eligible); Assert.Contains("override", result.Reason);
    }
    [Fact] public void ReviewedInclusionBeatsMissingMetadataAndRemainsReviewed()
    {
        var result = Eval("family", Movie("Drama"), new() { Facets = [new() { Name = "family_intent", Reviewed = true, Confidence = .9, ReviewedAt = Now }] });
        Assert.True(result.Eligible); Assert.Equal("reviewed_label", result.MatchSource);
    }
    [Fact] public void ProviderGenresOnlyFillMissingLocalGenres()
    {
        var work = new WorkEvidence { Metadata = new() { Genres = ["Family"] } };
        Assert.True(Eval("family", Movie("") ,work).Eligible);
        Assert.False(Eval("family", Movie("Drama"), work).Eligible);
    }
    [Fact] public void TagsAreUsableWithoutProviderOrManualEvidence() => Assert.True(Eval("books", Movie("Drama") with { Tags = ["Based_On_Novel"] }).Eligible);
    [Fact] public void MixedReviewedAndAutomaticCasualSignalsExplainBothSources()
    {
        var result = Eval("casual", Series("Comedy"), new() { Facets = [new() { Name = "low_cognitive_load", Reviewed = true, Confidence = .9, ReviewedAt = Now }] });
        Assert.True(result.Eligible); Assert.Equal("automatic_metadata", result.MatchSource);
        Assert.Contains("Reviewed label", result.Reason); Assert.Contains("Automatic metadata", result.Reason);
    }
    [Fact] public void AutomaticModeDoesNotGuessAwardsOrPilotQuality()
    {
        foreach (var key in new[] { "oscars", "globes", "emmys", "opening", "firstseason" })
            Assert.False(Eval(key, key is "opening" or "firstseason" ? Series("Comedy") : Movie()).Eligible);
    }
    [Fact] public void EndedMiniseriesStillNeedsEveryRequiredEpisode()
    {
        var series = Series("Drama");
        var work = new WorkEvidence { Metadata = new() { SeriesType = "Miniseries", Status = "Ended" }, Manifest = new() {
            WorkKey = series.WorkKey, Complete = true, Finished = true, FetchedAt = Now, Episodes = [new() { Season = 1, Number = 1, AirDate = Now.AddDays(-1) }] } };
        Assert.True(Eval("onenight", series, work).Eligible);
        Assert.False(Eval("onenight", series, work with { Manifest = work.Manifest with { Episodes = [.. work.Manifest.Episodes, new() { Season = 1, Number = 2, AirDate = Now.AddDays(-1) }] } }).Eligible);
        Assert.False(Eval("onenight", series, work with { Metadata = work.Metadata with { Status = "Canceled" } }).Eligible);
    }
    [Fact] public void WatchedAccessAndExplicitHideRemainAuthoritative()
    {
        Assert.False(Eval("family", Movie() with { Started = true }).Eligible);
        Assert.False(Eval("family", Movie() with { Available = false }).Eligible);
        Assert.False(Eval("family", Movie(), new() { Excluded = true }).Eligible);
    }
    [Fact] public void OldConfigurationEnablesAutomaticModeByDefault() => Assert.True(new PluginConfiguration().UseAutomaticMetadata);
    [Fact] public void ProviderDetailsProduceKeywordsAndCatalogueMetadataWithoutReviewedLabels()
    {
        using var json = JsonDocument.Parse("""{"vote_average":8,"vote_count":10000,"popularity":20,"genres":[{"name":"Family"}],"production_countries":[{"iso_3166_1":"GB"}],"release_date":"1985-01-02","keywords":{"keywords":[{"name":"based on novel"}]}}""");
        var work = TmdbEnrichmentService.ParseDetails("movie:tmdb:1", json.RootElement, Now);
        Assert.Empty(work.Facets); Assert.Equal(1985, work.Metadata!.Year); Assert.Equal(["Family"], work.Metadata.Genres);
        Assert.True(Eval("books", Movie("Drama"), work).Eligible);
        Assert.Empty(EvidenceValidator.Validate(new() { Items = [work] }, Now));
    }
    [Fact] public void TvDetailsReadKeywordResultsOriginAndSeriesType()
    {
        using var json = JsonDocument.Parse("""{"vote_average":8,"vote_count":10000,"popularity":20,"origin_country":["GB"],"first_air_date":"2024-01-02","type":"Miniseries","status":"Ended","keywords":{"results":[{"name":"baking"}]}}""");
        var work = TmdbEnrichmentService.ParseDetails("series:tmdb:1", json.RootElement, Now);
        Assert.Equal(["GB"], work.Metadata!.Countries); Assert.Equal("Miniseries",work.Metadata.SeriesType); Assert.Contains("baking",work.Keywords);
    }
    [Fact] public void WholeLibraryEnrichmentDoesNotStopAtOneBatchAndBackfillsOldRecords()
    {
        var items = Enumerable.Range(1, 205).Select(n => Movie() with { WorkKey = "movie:tmdb:" + n }).ToArray();
        var old = new Dictionary<string, WorkEvidence> { [items[0].WorkKey] = new() { ProviderFetchedAt = Now },
            [items[1].WorkKey] = new() { ProviderFetchedAt = Now, Metadata = new() } };
        var batches = TmdbEnrichmentService.PendingBatches(items.Concat([items[0], Movie() with { WorkKey = "movie:imdb:tt123" }]), old, Now, 100);
        Assert.Equal([100,100,4], batches.Select(b => b.Length)); Assert.Contains(items[0], batches.SelectMany(b => b)); Assert.DoesNotContain(items[1], batches.SelectMany(b => b));
    }
}
