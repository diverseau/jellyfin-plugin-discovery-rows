using System.Text.Json;
using DiscoveryRows.Core;
using Jellyfin.Plugin.DiscoveryRows.Services;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DiscoveryRows.Tests;

public sealed class TestPaths(string root) : IApplicationPaths
{
    public string ProgramDataPath => root; public string WebPath => root; public string ProgramSystemPath => root;
    public string DataPath => root; public string ImageCachePath => root; public string PluginsPath => root;
    public string PluginConfigurationsPath => root; public string LogDirectoryPath => root; public string ConfigurationDirectoryPath => root;
    public string SystemConfigurationFilePath => Path.Combine(root, "system.xml"); public string CachePath => root;
    public string TempDirectory => root; public string VirtualDataPath => root; public string TrickplayPath => root; public string BackupPath => root;
    public void MakeSanityCheckOrThrow() { }
    public void CreateAndCheckMarker(string path, string markerName, bool recursive = false) { }
}

public sealed class PersistenceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "discovery-rows-test-" + Guid.NewGuid().ToString("N"));
    private EvidenceStore NewStore() => new(new TestPaths(_root), NullLogger<EvidenceStore>.Instance);
    private static WorkEvidence Work(string key = "movie:tmdb:1") => new() { WorkKey = key, Excluded = true, Facets = [new() { Name = "uplifting", Reviewed = true, Confidence = .9, ReviewedAt = DateTimeOffset.UtcNow }] };
    [Fact] public void ImportsSurviveReloadAndPreserveUnrelatedRecords()
    {
        var store = NewStore(); store.Import(new() { Items = [Work()] }); store.Import(new() { Items = [Work("movie:tmdb:2")] });
        var reloaded = NewStore(); Assert.Equal(2, reloaded.Snapshot.Count); Assert.True(reloaded.Snapshot["movie:tmdb:1"].Excluded);
    }
    [Fact] public void InvalidBatchDoesNotPartiallyWrite()
    {
        var store = NewStore(); store.Import(new() { Items = [Work()] });
        Assert.Throws<ArgumentException>(() => store.Import(new() { Items = [Work("movie:tmdb:2"), new() { WorkKey = "actor:tmdb:3" }] }));
        Assert.Single(NewStore().Snapshot); Assert.False(store.Snapshot.ContainsKey("movie:tmdb:2"));
    }
    [Fact] public void BackgroundEnrichmentPreservesReviewedFieldsAndAwards()
    {
        var store = NewStore(); var work = Work() with { Awards = [new() { WorkKey = "movie:tmdb:1", Body = "academy", Category = "Picture", CeremonyYear = 2025, Outcome = "winner", EvidenceUrl = "https://www.oscars.org/oscars/ceremonies/2025" }] };
        store.Import(new() { Items = [work] });
        store.UpdateProvider(new() { WorkKey = work.WorkKey, Popularity = 5, PopularityProvider = "tmdb", ProviderFetchedAt = DateTimeOffset.UtcNow, Rating = new() { Value = 8, Votes = 10000, FetchedAt = DateTimeOffset.UtcNow } });
        var current = NewStore().Snapshot[work.WorkKey]; Assert.True(current.Excluded); Assert.Equal(work.Facets, current.Facets); Assert.Equal(work.Awards, current.Awards); Assert.NotNull(current.Rating);
    }
    [Fact] public void CorruptEvidenceIsPreservedAndBackedUpOnRepair()
    {
        var store = NewStore(); var file = Path.Combine(store.DataFolder, "evidence.json"); File.WriteAllText(file, "corrupt-file");
        var corrupt = NewStore(); Assert.NotNull(corrupt.LoadError); Assert.Equal("corrupt-file", File.ReadAllText(file));
        corrupt.Import(new() { Items = [Work()] }); Assert.Null(corrupt.LoadError); Assert.Single(Directory.GetFiles(store.DataFolder, "evidence.json.recovery-*")); Assert.Single(NewStore().Snapshot);
    }
    [Fact] public void FacetCorrectionsSurviveReloadAndDoNotLoseConcurrentProviderUpdates()
    {
        var store = NewStore();
        Parallel.Invoke(() => store.SetFacetOverride("movie:tmdb:1", "family_intent", false),
            () => store.UpdateProvider(new() { WorkKey = "movie:tmdb:1", Metadata = new() { Genres = ["Family"] },
                Rating = new() { Value = 8, Votes = 10000, FetchedAt = DateTimeOffset.UtcNow } }));
        var work = NewStore().Snapshot["movie:tmdb:1"];
        Assert.False(work.Facets.Single().Positive); Assert.NotNull(work.Rating); Assert.NotNull(work.Metadata);
        store.SetFacetOverride(work.WorkKey, "family_intent", null);
        work = NewStore().Snapshot[work.WorkKey];
        Assert.Empty(work.Facets); Assert.NotNull(work.Rating); Assert.NotNull(work.Metadata);
    }
    [Fact] public void SnapshotDoesNotChangeWhenStorePublishesNewRecords()
    {
        var store = NewStore(); store.Import(new() { Items = [Work()] }); var before = store.Snapshot;
        store.Import(new() { Items = [Work("movie:tmdb:2")] }); Assert.Single(before); Assert.Equal(2, store.Snapshot.Count);
    }
    [Fact] public void TokenIsSeparateFromEvidenceAndClearable()
    {
        var store = NewStore(); store.Import(new() { Items = [Work()] }); store.SetToken("test-token-value");
        Assert.True(store.HasToken); Assert.Equal("test-token-value", store.ReadToken());
        Assert.DoesNotContain("test-token-value", File.ReadAllText(Path.Combine(store.DataFolder, "evidence.json")));
        if (!OperatingSystem.IsWindows()) Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(Path.Combine(store.DataFolder, "tmdb-token")));
        store.SetToken(null); Assert.False(store.HasToken);
    }
    [Fact] public void DeliveryHistoryDoesNotImmediatelyInvalidateCurrentBucket()
    {
        var store = NewStore(); var history = new UserHistoryStore(store); var user = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        history.Record(user, ["movie:tmdb:1"], now); Assert.Empty(history.Recent(user, now));
        Assert.Contains("movie:tmdb:1", history.Recent(user, now.AddDays(1)));
        Assert.Empty(history.Recent(Guid.NewGuid(), now.AddDays(1)));
        Assert.Contains("movie:tmdb:1", new UserHistoryStore(store).Recent(user, now.AddDays(1)));
    }
    [Fact] public void FeedbackIsScopedToUserAndSurvivesRestart()
    {
        var store = NewStore(); var history = new UserHistoryStore(store); var user = Guid.NewGuid();
        history.SetFeedback(user, "movie:tmdb:1", true); Assert.Contains("movie:tmdb:1", new UserHistoryStore(store).Hidden(user)); Assert.Empty(history.Hidden(Guid.NewGuid()));
        history.SetFeedback(user, "movie:tmdb:1", false); Assert.Empty(history.Hidden(user));
    }
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
