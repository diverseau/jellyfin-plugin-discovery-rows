using System.Collections.Immutable;
using DiscoveryRows.Core;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.DiscoveryRows.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DiscoveryRows.Services;

public sealed record LibrarySnapshot(long Revision, DateTimeOffset At, IReadOnlyDictionary<Guid, MediaCandidate> Items);

public sealed class LibrarySnapshotService : IDisposable
{
    private readonly ILibraryManager _library;
    private readonly IUserDataManager _userData;
    private readonly ILogger<LibrarySnapshotService> _logger;
    private readonly SemaphoreSlim _refresh = new(1, 1);
    private LibrarySnapshot _snapshot = new(0, DateTimeOffset.MinValue, ImmutableDictionary<Guid, MediaCandidate>.Empty);
    private long _changeRevision;
    private long _builtChangeRevision = -1;
    public LibrarySnapshot Snapshot => _snapshot;
    public bool Dirty => Interlocked.Read(ref _changeRevision) != Interlocked.Read(ref _builtChangeRevision);
    public string? LastError { get; private set; }

    public LibrarySnapshotService(ILibraryManager library, IUserDataManager userData, ILogger<LibrarySnapshotService> logger)
    {
        _library = library; _userData = userData; _logger = logger;
        _library.ItemAdded += OnChanged; _library.ItemUpdated += OnChanged; _library.ItemRemoved += OnChanged;
    }
    private void OnChanged(object? sender, ItemChangeEventArgs args) => Interlocked.Increment(ref _changeRevision);
    public void MarkDirty() => Interlocked.Increment(ref _changeRevision);

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await _refresh.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var startRevision = Interlocked.Read(ref _changeRevision);
            var next = await Task.Run(() => Build(cancellationToken), cancellationToken).ConfigureAwait(false);
            _snapshot = new(_snapshot.Revision + 1, DateTimeOffset.UtcNow, next.ToImmutableDictionary(i => i.Id));
            Interlocked.Exchange(ref _builtChangeRevision, startRevision);
            LastError = null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { LastError = "Library snapshot refresh failed; prior snapshot retained."; _logger.LogError(ex, "Discovery Rows library refresh failed"); }
        finally { _refresh.Release(); }
    }

    private MediaCandidate[] Build(CancellationToken cancellationToken)
    {
        var items = _library.GetItemList(new InternalItemsQuery
        {
            Recursive = true, IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series, BaseItemKind.Episode],
            IsVirtualItem = false, IsPlaceHolder = false, GroupByPresentationUniqueKey = false, EnableTotalRecordCount = false
        });
        var episodes = new Dictionary<Guid, List<LocalEpisode>>();
        foreach (var ep in items.OfType<Episode>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sources = Sources(ep);
            if (!episodes.TryGetValue(ep.SeriesId, out var group)) episodes[ep.SeriesId] = group = [];
            group.Add(new() { Id = ep.Id, Season = ep.ParentIndexNumber ?? -1, Number = ep.IndexNumber ?? -1,
                EndNumber = ep.IndexNumberEnd ?? ep.IndexNumber ?? -1, Accessible = sources.Available,
                Runtimes = sources.Runtimes, ProviderId = ep.ProviderIds.GetValueOrDefault("Tmdb") });
        }
        var result = new List<MediaCandidate>();
        foreach (var item in items.Where(i => i is Movie or Series))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var kind = item is Movie ? MediaKind.Movie : MediaKind.Series;
            var key = WorkKey(item, kind);
            var regular = episodes.GetValueOrDefault(item.Id)?.ToArray() ?? [];
            var sources = kind == MediaKind.Movie ? Sources(item) : (regular.Any(e => e.Accessible), Array.Empty<long?>());
            var season = item is Series ? _library.GetItemList(new InternalItemsQuery { ParentId = item.Id, IncludeItemTypes = [BaseItemKind.Season], IsVirtualItem = false }).OfType<Season>().FirstOrDefault(s => s.IndexNumber == 1)?.Id : null;
            result.Add(new() { Id = item.Id, WorkKey = key, Name = item.Name, Kind = kind, Available = sources.Item1,
                Runtimes = sources.Item2, Episodes = regular, Genres = item.Genres.ToArray(), Tags = item.Tags.ToArray(),
                AddedAt = new DateTimeOffset(DateTime.SpecifyKind(item.DateCreated, DateTimeKind.Utc)),
                EpisodeOrder = item is Series series && !string.IsNullOrEmpty(series.DisplayOrder) ? series.DisplayOrder.ToLowerInvariant() : "aired",
                FirstSeasonId = season, FranchiseKey = item.ProviderIds.GetValueOrDefault("TmdbCollection") });
        }
        return result.ToArray();
    }

    public static string WorkKey(BaseItem item, MediaKind kind)
    {
        var type = kind == MediaKind.Movie ? "movie" : "series";
        var id = item.ProviderIds.GetValueOrDefault("Tmdb");
        if (long.TryParse(id, out var number) && number > 0) return $"{type}:tmdb:{number}";
        var imdb = item.ProviderIds.GetValueOrDefault("Imdb");
        if (imdb is not null && EvidenceValidator.IsWorkKey($"{type}:imdb:{imdb}")) return $"{type}:imdb:{imdb}";
        return $"{type}:jellyfin:{item.Id:N}";
    }

    public static (bool Available, long?[] Runtimes) Sources(BaseItem item)
    {
        if (item.IsVirtualItem || item is Video { IsPlaceHolder: true } || item.SourceType == SourceType.Channel) return (false, []);
        var sources = item.GetMediaSources(false);
        return (sources.Count > 0 && sources.All(s => s.Protocol == MediaProtocol.File && !s.IsRemote && !string.IsNullOrEmpty(s.Path) && (File.Exists(s.Path) || Directory.Exists(s.Path))), sources.Select(s => s.RunTimeTicks).ToArray());
    }

    public MediaCandidate[] ForUser(User user, PluginConfiguration configuration, IEnumerable<Guid>? restrict = null)
    {
        // Query filtering plus standalone checks preserve both library-root and inherited parental/tag restrictions.
        var query = new InternalItemsQuery(user)
        {
            Recursive = true, IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series, BaseItemKind.Episode],
            IsVirtualItem = false, IsPlaceHolder = false, GroupByPresentationUniqueKey = false, EnableTotalRecordCount = false
        };
        var visible = _library.GetItemList(query).Where(i => i.IsVisibleStandalone(user)).ToDictionary(i => i.Id);
        var requested = restrict?.ToHashSet();
        var result = new List<MediaCandidate>();
        foreach (var item in _snapshot.Items.Values)
        {
            if (requested is not null && !requested.Contains(item.Id)) continue;
            if (!visible.TryGetValue(item.Id, out var current)) continue;
            var ud = _userData.GetUserData(user, current);
            var eps = item.Episodes.Select(e =>
            {
                if (!visible.TryGetValue(e.Id, out var actual)) return e with { Accessible = false };
                var data = _userData.GetUserData(user, actual);
                return e with { Accessible = e.Accessible && !actual.IsVirtualItem, Started = data?.Played == true || data?.PlaybackPositionTicks > 0 || data?.PlayCount > 0 };
            }).ToArray();
            var genres = current.Genres.Select(g => configuration.GenreAliases.FirstOrDefault(a => a.Source.Equals(g, StringComparison.OrdinalIgnoreCase))?.Canonical ?? g).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            result.Add(item with { Started = ud?.Played == true || ud?.PlaybackPositionTicks > 0 || ud?.PlayCount > 0 || eps.Any(e => e.Season > 0 && e.Started),
                Favourite = ud?.IsFavorite ?? false, Liked = ud?.Likes, LastPlayedAt = ud?.LastPlayedDate is { } played ? new DateTimeOffset(DateTime.SpecifyKind(played, DateTimeKind.Utc)) : null,
                Episodes = eps, Genres = genres, Tags = current.Tags.ToArray() });
        }
        return result.ToArray();
    }

    public MediaCandidate? Revalidate(User user, MediaCandidate cached, PluginConfiguration config)
    {
        var actual = _library.GetItemById(cached.Id);
        if (actual is null || !actual.IsVisibleStandalone(user) || actual.IsVirtualItem || WorkKey(actual, cached.Kind) != cached.WorkKey) return null;
        var ud = _userData.GetUserData(user, actual);
        var episodes = cached.Episodes.Select(e =>
        {
            var item = _library.GetItemById(e.Id);
            if (item is null || !item.IsVisibleStandalone(user)) return e with { Accessible = false };
            var source = Sources(item);
            var data = _userData.GetUserData(user, item);
            return e with { Accessible = source.Available, Runtimes = source.Runtimes,
                Started = data?.Played == true || data?.PlaybackPositionTicks > 0 || data?.PlayCount > 0 };
        }).ToArray();
        var currentSource = actual is Movie ? Sources(actual) : (episodes.Any(e => e.Accessible), Array.Empty<long?>());
        return cached with { Available = currentSource.Item1, Runtimes = currentSource.Item2, Episodes = episodes,
            Started = ud?.Played == true || ud?.PlaybackPositionTicks > 0 || ud?.PlayCount > 0 || episodes.Any(e => e.Season > 0 && e.Started),
            Genres = actual.Genres.Select(g => config.GenreAliases.FirstOrDefault(a => a.Source.Equals(g, StringComparison.OrdinalIgnoreCase))?.Canonical ?? g).ToArray() };
    }

    public void Dispose()
    {
        _library.ItemAdded -= OnChanged; _library.ItemUpdated -= OnChanged; _library.ItemRemoved -= OnChanged; _refresh.Dispose();
    }
}
