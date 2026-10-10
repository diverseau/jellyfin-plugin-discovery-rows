using DiscoveryRows.Core;
using Xunit;

namespace DiscoveryRows.Tests;

public sealed class SelectorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly RankingContext Context = new() { UserId = Guid.Parse("d0f07f58-3d32-44e9-9720-11875435055f"), Now = Now };
    private static MediaCandidate Movie(double minutes = 80) => new() { Id = Guid.NewGuid(), WorkKey = "movie:tmdb:1", Name = "Fixture", Kind = MediaKind.Movie, Available = true, AddedAt = Now.AddDays(-10), Runtimes = [TimeSpan.FromMinutes(minutes).Ticks], Genres = ["Comedy", "Thriller", "Documentary"] };
    private static LocalEpisode Episode(int number, double minutes = 25, int end = 0) => new() { Id = Guid.NewGuid(), Season = 1, Number = number, EndNumber = end == 0 ? number : end, Accessible = true, Runtimes = [TimeSpan.FromMinutes(minutes).Ticks] };
    private static MediaCandidate Series(params LocalEpisode[] episodes) => new() { Id = Guid.NewGuid(), WorkKey = "series:tmdb:2", Name = "Series fixture", Kind = MediaKind.Series, Available = true, Episodes = episodes, Genres = ["Comedy"] };
    private static WorkEvidence Reviewed(string key, string facet, double confidence = .95) => new() { WorkKey = key, Facets = [new() { Name = facet, Confidence = confidence, Reviewed = true, ReviewedAt = Now }] };
    private static Evaluation Evaluate(string row, MediaCandidate item, WorkEvidence? evidence = null, RowSettings? settings = null, RankingContext? context = null)
    {
        var definition = RowCatalogue.Find(row)!;
        return RowEvaluator.Evaluate(definition, settings ?? RowCatalogue.Defaults(definition) with { Enabled = true }, item, evidence, context ?? Context);
    }

    [Theory]
    [InlineData(89, 59, true)] [InlineData(90, 0, false)] [InlineData(90, 1, false)] [InlineData(91, 0, false)]
    public void StrictTimeBoundaryDoesNotRound(int minutes, int seconds, bool eligible)
    {
        var movie = Movie() with { Runtimes = [TimeSpan.FromMinutes(minutes).Ticks + TimeSpan.FromSeconds(seconds).Ticks] };
        Assert.Equal(eligible, Evaluate("under90", movie).Eligible);
    }
    [Theory]
    [InlineData(89, 59, true)] [InlineData(90, 0, true)] [InlineData(90, 1, false)]
    public void InclusiveTimeBoundary(int minutes, int seconds, bool eligible) => Assert.Equal(eligible, Evaluate("comedy90", Movie() with { Runtimes = [TimeSpan.FromMinutes(minutes).Ticks + TimeSpan.FromSeconds(seconds).Ticks] }).Eligible);
    [Fact] public void OneTickOverLimitFails() => Assert.False(Evaluate("comedy90", Movie() with { Runtimes = [TimeSpan.FromMinutes(90).Ticks + 1] }).Eligible);
    [Theory] [InlineData(null)] [InlineData(0L)] [InlineData(-1L)]
    public void UnknownOrInvalidRequiredRuntimeFails(long? ticks) => Assert.False(Evaluate("under90", Movie() with { Runtimes = [ticks] }).Eligible);
    [Fact] public void EverySelectableEditionMustFit() => Assert.False(Evaluate("under90", Movie() with { Runtimes = [TimeSpan.FromMinutes(88).Ticks, TimeSpan.FromMinutes(105).Ticks] }).Eligible);
    [Fact] public void MissingVersionRuntimeFailsTimePromise() => Assert.False(Evaluate("under90", Movie() with { Runtimes = [TimeSpan.FromMinutes(88).Ticks, null] }).Eligible);
    [Fact] public void EmptyRuntimeArrayFails() => Assert.False(Evaluate("under90", Movie() with { Runtimes = [] }).Eligible);
    [Fact] public void LongPilotCannotBeHiddenByShortMedian() => Assert.False(Evaluate("shortepisodes", Series(Episode(1, 60), Episode(2), Episode(3))).Eligible);
    [Fact] public void MissingPilotCannotBeReplacedByFirstLocalEpisode() => Assert.False(Evaluate("shortepisodes", Series(Episode(2), Episode(3))).Eligible);
    [Fact] public void InaccessiblePilotFails() => Assert.False(Evaluate("shortepisodes", Series(Episode(1) with { Accessible = false }, Episode(2))).Eligible);
    [Fact] public void InaccessibleRegularEpisodeFailsLiteralShortSeries() => Assert.False(Evaluate("shortepisodes", Series(Episode(1), Episode(2) with { Accessible = false })).Eligible);
    [Fact] public void SpecialsDoNotDistortShortEpisodePromise() => Assert.True(Evaluate("shortepisodes", Series(Episode(1), Episode(2), Episode(1, 100) with { Season = 0 })).Eligible);
    [Fact] public void WrongEpisodeOrderFailsStart() => Assert.False(Evaluate("shortepisodes", Series(Episode(1)) with { EpisodeOrder = "dvd" }).Eligible);
    [Fact] public void RequiredGenreIsNotInferredFromOtherGenres() => Assert.False(Evaluate("comedy90", Movie() with { Genres = ["Drama"] }).Eligible);
    [Fact] public void GenreComparisonIgnoresCase() => Assert.True(Evaluate("comedy90", Movie() with { Genres = ["comedy"] }).Eligible);

    [Theory]
    [InlineData("laughmovies", "humour")] [InlineData("popcorn", "popcorn")] [InlineData("feelgood", "uplifting")]
    [InlineData("chosenfamily", "found_family")] [InlineData("cyberpunk", "cyberpunk")] [InlineData("dystopia", "future_dystopia")]
    [InlineData("food", "food_central")] [InlineData("smalltown", "small_town_charm")] [InlineData("smalltownscares", "small_town_scary")]
    [InlineData("scams", "scams_central")] [InlineData("arthorror", "art_horror")] [InlineData("gentlereality", "gentle_british_reality")]
    [InlineData("witchcraft", "witchcraft_central")] [InlineData("books", "book_adaptation")] [InlineData("truestory", "dramatized_true_story")]
    [InlineData("family", "family_intent")] [InlineData("nostalgia80", "1980s_nostalgia")] [InlineData("twistedchristmas", "twisted_christmas")] [InlineData("cringe", "cringe_comedy")]
    public void EditorialRowsRequireReviewedEvidence(string row, string facet)
    {
        Assert.False(Evaluate(row, Movie()).Eligible);
        var evidence = Reviewed("movie:tmdb:1", facet);
        Assert.True(Evaluate(row, Movie(), evidence).Eligible);
        Assert.False(Evaluate(row, Movie(), evidence with { Facets = [evidence.Facets[0] with { Reviewed = false }] }).Eligible);
        Assert.False(Evaluate(row, Movie(), evidence with { Facets = [evidence.Facets[0] with { Positive = false }] }).Eligible);
        Assert.False(Evaluate(row, Movie(), evidence with { Facets = [evidence.Facets[0] with { Confidence = .1 }] }).Eligible);
    }
    [Fact] public void CasualNeedsEpisodicEvidenceToo()
    {
        var evidence = Reviewed("series:tmdb:2", "low_cognitive_load");
        Assert.False(Evaluate("casual", Series(Episode(1)), evidence).Eligible);
        Assert.True(Evaluate("casual", Series(Episode(1)), evidence with { Facets = [.. evidence.Facets, new() { Name = "episodic", Reviewed = true, Confidence = .9 }] }).Eligible);
    }
    [Fact] public void StrongOpeningRequiresAccessiblePilotAndReview()
    {
        var evidence = Reviewed("series:tmdb:2", "strong_opening");
        Assert.True(Evaluate("opening", Series(Episode(1)), evidence).Eligible);
        Assert.False(Evaluate("opening", Series(Episode(2)), evidence).Eligible);
    }
    [Fact] public void WatchedAndExplicitExclusionsWinOverScores()
    {
        Assert.False(Evaluate("under90", Movie() with { Started = true }).Eligible);
        Assert.False(Evaluate("under90", Movie(), new() { Excluded = true }).Eligible);
        Assert.False(Evaluate("under90", Movie(), context: Context with { HiddenWorks = new HashSet<string> { "movie:tmdb:1" } }).Eligible);
        Assert.False(Evaluate("under90", Movie() with { Available = false }).Eligible);
    }
    [Fact] public void TinySampleDoesNotQualifyHighQuality()
    {
        var evidence = new WorkEvidence { Rating = new() { Value = 9.8, Votes = 5, FetchedAt = Now } };
        Assert.False(Evaluate("quality", Movie(), evidence).Eligible);
        Assert.True(Evaluate("quality", Movie(), evidence with { Rating = evidence.Rating with { Value = 8, Votes = 10000 } }).Eligible);
    }
    [Fact] public void RatingWithoutPairedVotesCannotQualify() => Assert.False(Evaluate("quality", Movie(), new() { Rating = new() { Value = 9.8, Votes = null, FetchedAt = Now } }).Eligible);
    [Fact] public void RatingScaleNormalizesWithoutMixingProviders()
    {
        var settings = RowCatalogue.Defaults(RowCatalogue.Find("quality")!);
        var ten = RowEvaluator.AdjustedRating(new() { Value = 8, Scale = 10, Votes = 10000, FetchedAt = Now }, settings, Now);
        var hundred = RowEvaluator.AdjustedRating(new() { Value = 80, Scale = 100, Votes = 10000, FetchedAt = Now }, settings, Now);
        Assert.Equal(ten, hundred);
    }
    [Fact] public void ExpiredRatingDoesNotPromiseQuality() => Assert.False(Evaluate("quality", Movie(), new() { Rating = new() { Value = 9, Votes = 10000, FetchedAt = Now.AddDays(-91) } }).Eligible);
    [Fact] public void GemsNeedQualityAndRelativeExposure()
    {
        var e = new WorkEvidence { Rating = new() { Value = 8, Votes = 10000, FetchedAt = Now }, Popularity = 5, PopularityProvider = "tmdb", ProviderFetchedAt = Now };
        Assert.False(Evaluate("gems", Movie(), e).Eligible);
        Assert.True(Evaluate("gems", Movie(), e, context: Context with { ExposureCutoff = 10 }).Eligible);
        Assert.False(Evaluate("gems", Movie(), e, context: Context with { ExposureCutoff = 1 }).Eligible);
        Assert.False(Evaluate("gems", Movie(), e with { PopularityProvider = "other_provider" }, context: Context with { ExposureCutoff = 10 }).Eligible);
    }
    [Fact] public void HiddenLibraryDoesNotRequireGlobalPopularity() => Assert.True(Evaluate("hidden", Movie()).Eligible);
    [Fact] public void DiscoveryCannotBeChangedIntoRewatchByGenericSetting() => Assert.False(Evaluate("hidden", Movie() with { Started = true }, settings: RowCatalogue.Defaults(RowCatalogue.Find("hidden")!) with { ExcludeStarted = false }).Eligible);
    [Fact] public void RecentDeliveryExcludesDiscovery() => Assert.False(Evaluate("hidden", Movie(), context: Context with { RecentlyDelivered = new HashSet<string> { "movie:tmdb:1" } }).Eligible);

    private static EpisodeManifest Manifest(params ManifestEpisode[] episodes) => new() { WorkKey = "series:tmdb:2", Complete = true, Finished = true, FetchedAt = Now, Episodes = episodes };
    private static ManifestEpisode Required(int n) => new() { Season = 1, Number = n, AirDate = Now.AddDays(-1) };
    [Fact] public void MissingFinaleCannotQualifyCompleteStory()
    {
        var e = Reviewed("series:tmdb:2", "complete_limited_story") with { Manifest = Manifest(Required(1), Required(2), Required(3)) };
        Assert.False(Evaluate("onenight", Series(Episode(1), Episode(2)), e).Eligible);
    }
    [Fact] public void DoubleEpisodeFileCountsDurationOnce()
    {
        var s = Series(Episode(1, 50, 2)); var e = Manifest(Required(1), Required(2));
        var complete = RowEvaluator.CompleteRuntime(s, e, Now);
        Assert.True(complete.Complete); Assert.Equal(TimeSpan.FromMinutes(50).Ticks, complete.Ticks);
    }
    [Theory] [InlineData(239, true)] [InlineData(240, true)] [InlineData(241, false)]
    public void WholeStoryTimeBudget(int minutes, bool eligible)
    {
        var e = Reviewed("series:tmdb:2", "complete_limited_story") with { Manifest = Manifest(Required(1)) };
        Assert.Equal(eligible, Evaluate("onenight", Series(Episode(1, minutes)), e).Eligible);
    }
    [Fact] public void CanceledStatusAloneDoesNotProveResolvedStory()
    {
        var e = new WorkEvidence { Manifest = Manifest(Required(1)) };
        Assert.False(Evaluate("onenight", Series(Episode(1)), e).Eligible);
        Assert.True(Evaluate("complete", Series(Episode(1)), e).Eligible);
    }
    [Fact] public void StaleUnknownOrUnairedManifestFails()
    {
        var series = Series(Episode(1)); var manifest = Manifest(Required(1));
        Assert.False(RowEvaluator.CompleteRuntime(series, manifest with { FetchedAt = Now.AddDays(-31) }, Now).Complete);
        Assert.False(RowEvaluator.CompleteRuntime(series, manifest with { Complete = false }, Now).Complete);
        Assert.False(RowEvaluator.CompleteRuntime(series, manifest with { Episodes = [Required(1) with { AirDate = null }] }, Now).Complete);
        Assert.False(RowEvaluator.CompleteRuntime(series, manifest with { Episodes = [Required(1) with { AirDate = Now.AddDays(1) }] }, Now).Complete);
        Assert.False(RowEvaluator.CompleteRuntime(series, manifest with { WorkKey = "series:tmdb:9" }, Now).Complete);
    }
    [Fact] public void RequiredInaccessibleEpisodeFailsCompleteness() => Assert.False(RowEvaluator.CompleteRuntime(Series(Episode(1), Episode(2) with { Accessible = false }), Manifest(Required(1), Required(2)), Now).Complete);
    [Fact] public void ConflictingProviderEpisodeIdIsNotMatched() => Assert.False(RowEvaluator.CompleteRuntime(Series(Episode(1) with { ProviderId = "111" }), Manifest(Required(1) with { ProviderId = "222" }), Now).Complete);
    [Fact] public void FirstSeasonRequiresRealSeasonCard()
    {
        var e = Reviewed("series:tmdb:2", "great_first_season") with { Manifest = Manifest(Required(1)) };
        Assert.False(Evaluate("firstseason", Series(Episode(1)), e).Eligible);
        Assert.True(Evaluate("firstseason", Series(Episode(1)) with { FirstSeasonId = Guid.NewGuid() }, e).Eligible);
    }
    [Fact] public void PersonAwardDoesNotQualifyUnrelatedWork()
    {
        var award = new AwardRecord { WorkKey = "movie:tmdb:99", Body = "academy", Outcome = "winner", Category = "Actor", CeremonyYear = 2025 };
        Assert.False(Evaluate("oscars", Movie(), new() { Awards = [award] }).Eligible);
        Assert.True(Evaluate("oscars", Movie(), new() { Awards = [award with { WorkKey = "movie:tmdb:1" }] }).Eligible);
    }
    [Fact] public void NominationAndWinnerAndCeremonyYearRemainDistinct()
    {
        var award = new AwardRecord { WorkKey = "movie:tmdb:1", Body = "golden_globes", Outcome = "nominee", Category = "Picture", CeremonyYear = 2025 };
        var e = new WorkEvidence { Awards = [award] };
        Assert.True(Evaluate("globes", Movie(), e).Eligible);
        Assert.False(Evaluate("oscars", Movie(), e with { Awards = [award with { Body = "academy" }] }).Eligible);
        var settings = RowCatalogue.Defaults(RowCatalogue.Find("globes")!) with { Enabled = true, AwardYears = [2024] };
        Assert.False(Evaluate("globes", Movie(), e, settings).Eligible);
    }
    [Fact] public void LogicalTitleDeduplicationHappensBeforeAllocation()
    {
        var row = RowCatalogue.Find("under90")!; var settings = RowCatalogue.Defaults(row);
        var candidates = RowEvaluator.Rank(row, settings, [Movie(), Movie()], new Dictionary<string, WorkEvidence>(), Context);
        Assert.Single(candidates);
    }
    [Fact] public void DailyOrderingIsStableAndUserSpecific()
    {
        var row = RowCatalogue.Find("under90")!; var settings = RowCatalogue.Defaults(row);
        var items = Enumerable.Range(1, 30).Select(n => Movie() with { WorkKey = $"movie:tmdb:{n}" }).ToArray();
        var a = RowEvaluator.Rank(row, settings, items, new Dictionary<string, WorkEvidence>(), Context).Select(r => r.Item.WorkKey).ToArray();
        var b = RowEvaluator.Rank(row, settings, items.Reverse(), new Dictionary<string, WorkEvidence>(), Context).Select(r => r.Item.WorkKey).ToArray();
        var c = RowEvaluator.Rank(row, settings, items, new Dictionary<string, WorkEvidence>(), Context with { UserId = Guid.NewGuid() }).Select(r => r.Item.WorkKey).ToArray();
        Assert.Equal(a, b); Assert.NotEqual(a, c);
    }
}
