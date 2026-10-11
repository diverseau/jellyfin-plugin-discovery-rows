using System.Diagnostics;
using DiscoveryRows.Core;
using Jellyfin.Data;
using Jellyfin.Data.Events;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.DiscoveryRows.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DiscoveryRows.Services;

public sealed record UserPlan(string Signature, DateTimeOffset Created, RankingContext Context, bool Coordinated,
    IReadOnlyDictionary<string, RankedCandidate[]> Candidates, IReadOnlyDictionary<string, RankedCandidate[]> Allocation);

public sealed class RecommendationService : IDisposable
{
    private readonly LibrarySnapshotService _index;
    private readonly EvidenceStore _evidence;
    private readonly UserHistoryStore _history;
    private readonly IUserManager _users;
    private readonly IUserDataManager _userData;
    private readonly ILibraryManager _library;
    private readonly IDtoService _dto;
    private readonly HssBridge _hss;
    private readonly ILogger<RecommendationService> _logger;
    private readonly object _gate = new();
    private readonly Dictionary<Guid, UserPlan> _cache = [];
    private long _revision;
    private long _requests;
    private double _lastMilliseconds;
    public long Requests => Interlocked.Read(ref _requests);
    public double LastMilliseconds => _lastMilliseconds;
    public int CachedUsers { get { lock (_gate) return _cache.Count; } }

    public RecommendationService(LibrarySnapshotService index, EvidenceStore evidence, UserHistoryStore history,
        IUserManager users, IUserDataManager userData, ILibraryManager library, IDtoService dto, HssBridge hss, ILogger<RecommendationService> logger)
    {
        _index = index; _evidence = evidence; _history = history; _users = users; _userData = userData; _library = library; _dto = dto; _hss = hss; _logger = logger;
        _userData.UserDataSaved += OnUserData; _users.OnUserUpdated += OnUserUpdated;
    }
    private void OnUserData(object? sender, UserDataSaveEventArgs args) => Invalidate();
    private void OnUserUpdated(object? sender, GenericEventArgs<User> args) => Invalidate();
    public void Invalidate() { Interlocked.Increment(ref _revision); lock (_gate) _cache.Clear(); }

    public User? GetUser(Guid id)
    {
        if (id == Guid.Empty) return null;
        var user = _users.GetUserById(id);
        return user is null || user.HasPermission(PermissionKind.IsDisabled) ? null : user;
    }

    public QueryResult<BaseItemDto> Results(Guid userId, string key, bool recordDelivery = true, bool enforceSelection = false)
    {
        var stopwatch = Stopwatch.StartNew();
        Interlocked.Increment(ref _requests);
        try
        {
            var user = GetUser(userId);
            var row = RowCatalogue.Find(key);
            var config = Plugin.Instance?.Configuration;
            if (user is null || row is null || config?.Enabled != true || _index.Snapshot.Revision == 0) return Empty();
            var settings = config.GetRow(row);
            if (!settings.Enabled) return Empty();
            // HSS can retain page metadata after a user disables a row. Honor the current effective selection.
            if (enforceSelection && _hss.EffectiveRows(userId) is { } selected && !selected.Contains(key, StringComparer.Ordinal)) return Empty();
            var revision = Interlocked.Read(ref _revision);
            var plan = Plan(user, config, key);
            var assigned = plan.Allocation.GetValueOrDefault(key) ?? [];
            if (assigned.Length == 0) return Empty();
            var other = plan.Allocation.Where(r => r.Key != key).SelectMany(r => r.Value).Select(r => r.Item.WorkKey).ToHashSet(StringComparer.Ordinal);
            var results = new List<MediaCandidate>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var franchises = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var r in assigned.Concat(plan.Candidates.GetValueOrDefault(key) ?? []))
            {
                if (!seen.Add(r.Item.WorkKey) || plan.Coordinated && other.Contains(r.Item.WorkKey)) continue;
                var current = _index.Revalidate(user, r.Item, config);
                if (current is null || !RowEvaluator.Evaluate(row, settings, current, _evidence.Snapshot.GetValueOrDefault(current.WorkKey), plan.Context).Eligible) continue;
                if (current.FranchiseKey is { } franchise)
                {
                    if (franchises.GetValueOrDefault(franchise) >= settings.FranchiseCap) continue;
                    franchises[franchise] = franchises.GetValueOrDefault(franchise) + 1;
                }
                results.Add(current);
                if (results.Count == settings.Limit) break;
            }
            if (results.Count < settings.Minimum || revision != Interlocked.Read(ref _revision) || GetUser(userId) is null) return Empty();
            var items = results.Select(i => _library.GetItemById(row.Selector == SelectorKind.FirstSeason ? i.FirstSeasonId!.Value : i.Id))
                .Where(i => i is not null && i.IsVisibleStandalone(user)).Select(i => i!).ToArray();
            if (items.Length < settings.Minimum) return Empty();
            var dtos = _dto.GetBaseItemDtos(items, new DtoOptions { Fields = [ItemFields.PrimaryImageAspectRatio, ItemFields.MediaSourceCount, ItemFields.Genres, ItemFields.ProviderIds] }, user);
            if (revision != Interlocked.Read(ref _revision)) return Empty();
            if (recordDelivery) _history.Record(userId, results.Select(i => i.WorkKey), DateTimeOffset.UtcNow);
            return new QueryResult<BaseItemDto>(dtos);
        }
        catch (Exception ex)
        { _logger.LogWarning(ex, "Discovery Rows row {Row} failed safely", key); return Empty(); }
        finally { _lastMilliseconds = stopwatch.Elapsed.TotalMilliseconds; }
    }

    private static QueryResult<BaseItemDto> Empty() => new(Array.Empty<BaseItemDto>());

    public UserPlan Plan(User user, PluginConfiguration config, string? requestedRow = null)
    {
        var effective = config.CoordinateRows ? _hss.EffectiveRows(user.Id) : null;
        var keys = effective is not null ? effective : RowCatalogue.All.Select(r => r.Key).ToArray();
        // Preview or direct calls may ask for a shelf absent from the selected page. Keep that independent.
        if (requestedRow is not null && !keys.Contains(requestedRow, StringComparer.Ordinal)) keys = [requestedRow];
        var now = DateTimeOffset.UtcNow;
        var signature = $"{Interlocked.Read(ref _revision)}|{_index.Snapshot.Revision}|{_evidence.Revision}|{_history.Revision}|{now.UtcDateTime:yyyy-MM-dd}|{string.Join(',', keys)}";
        lock (_gate)
        {
            if (_cache.TryGetValue(user.Id, out var cached) && cached.Signature == signature && now - cached.Created < TimeSpan.FromMinutes(config.CacheMinutes)) return cached;
            var media = _index.ForUser(user, config);
            var scores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in media)
            {
                var weight = item.Favourite ? 2.5 : item.Liked == true ? 2 : item.Liked == false ? -.5 : item.Started ? .4 : 0;
                if (item.LastPlayedAt is { } at && weight > 0) weight *= .5 + .5 * Math.Exp(-(now - at).TotalDays / 90);
                foreach (var genre in item.Genres) scores[genre] = scores.GetValueOrDefault(genre) + weight;
            }
            var max = Math.Max(1, scores.Values.DefaultIfEmpty(1).Max());
            var evidence = _evidence.Snapshot;
            var popularity = media.Where(m => m.Kind == MediaKind.Movie).GroupBy(m => m.WorkKey, StringComparer.Ordinal).Select(g => evidence.GetValueOrDefault(g.Key)).Where(e => e?.ProviderFetchedAt >= now.AddDays(-90) && e.PopularityProvider == "tmdb" && e.Popularity is >= 0).Select(e => e!.Popularity!.Value).Order().ToArray();
            var context = new RankingContext { UserId = user.Id, Now = now, UseAutomaticMetadata = config.UseAutomaticMetadata, GenreTaste = scores.ToDictionary(s => s.Key, s => Math.Clamp(.5 + .45 * s.Value / max, 0, 1), StringComparer.OrdinalIgnoreCase),
                HiddenWorks = _history.Hidden(user.Id), RecentlyDelivered = _history.Recent(user.Id, now),
                ExposureCutoff = popularity.Length >= 50 ? popularity[(int)Math.Floor((popularity.Length - 1) * .4)] : null };
            var ranked = RowCatalogue.All.Where(r => keys.Contains(r.Key, StringComparer.Ordinal) && config.GetRow(r).Enabled).Select(r => (Settings: config.GetRow(r), Candidates: RowEvaluator.Rank(r, config.GetRow(r), media, evidence, context))).ToList();
            var coordinated = config.CoordinateRows && effective is not null && keys.Length > 1;
            var allocation = RowAllocator.Allocate(ranked, coordinated);
            var plan = new UserPlan(signature, now, context, coordinated, ranked.ToDictionary(r => r.Settings.Key, r => r.Candidates), allocation);
            if (_cache.Count >= config.MaximumCachedUsers && !_cache.ContainsKey(user.Id)) _cache.Remove(_cache.OrderBy(c => c.Value.Created).First().Key);
            _cache[user.Id] = plan;
            return plan;
        }
    }

    public object Availability(Guid userId)
    {
        var user = GetUser(userId) ?? throw new ArgumentException("User unavailable.");
        var config = Plugin.Instance?.Configuration ?? throw new InvalidOperationException("Plugin unavailable.");
        var media = _index.ForUser(user, config);
        var context = Plan(user, config).Context;
        return RowCatalogue.All.Select(r => new { key = r.Key, title = r.Title,
            candidates = RowEvaluator.Rank(r, config.GetRow(r) with { Enabled = true }, media, _evidence.Snapshot, context).Length }).ToArray();
    }

    public object Coverage(Guid userId, string? key = null, int take = 20)
    {
        var user = GetUser(userId) ?? throw new ArgumentException("User unavailable.");
        var config = Plugin.Instance?.Configuration ?? throw new InvalidOperationException("Plugin unavailable.");
        var media = _index.ForUser(user, config);
        var plan = Plan(user, config, key);
        return RowCatalogue.All.Where(r => key is null || r.Key == key).Select(r =>
        {
            var settings = config.GetRow(r);
            var evaluations = media.Select(m => new { item = m, evaluation = RowEvaluator.Evaluate(r, settings, m, _evidence.Snapshot.GetValueOrDefault(m.WorkKey), plan.Context) }).ToArray();
            return new { key = r.Key, title = r.Title, sectionId = r.SectionId, settings, candidates = evaluations.Count(e => e.evaluation.Eligible),
                visible = plan.Allocation.GetValueOrDefault(r.Key)?.Length ?? 0,
                reasons = evaluations.Where(e => !e.evaluation.Eligible).GroupBy(e => e.evaluation.Reason).ToDictionary(g => g.Key, g => g.Count()),
                examples = evaluations.Where(e => e.evaluation.Eligible).OrderByDescending(e => e.evaluation.Score).Take(Math.Clamp(take, 0, 40)).Select(e => new { id = e.item.Id, workKey = e.item.WorkKey, name = e.item.Name, explanation = e.evaluation }) };
        }).ToArray();
    }

    public void Dispose() { _userData.UserDataSaved -= OnUserData; _users.OnUserUpdated -= OnUserUpdated; }
}
