using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using DiscoveryRows.Core;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DiscoveryRows.Services;

public sealed class TmdbEnrichmentService
{
    private readonly IHttpClientFactory _clients;
    private readonly EvidenceStore _evidence;
    private readonly LibrarySnapshotService _index;
    private readonly RecommendationService _recommendations;
    private readonly ILogger<TmdbEnrichmentService> _logger;
    private readonly SemaphoreSlim _run = new(1, 1);
    private DateTimeOffset _lastRequest = DateTimeOffset.MinValue;
    private bool _authFailed;
    public string Status { get; private set; } = "Optional enrichment has not run";
    public long RequestCount { get; private set; }

    public TmdbEnrichmentService(IHttpClientFactory clients, EvidenceStore evidence, LibrarySnapshotService index,
        RecommendationService recommendations, ILogger<TmdbEnrichmentService> logger)
    { _clients = clients; _evidence = evidence; _index = index; _recommendations = recommendations; _logger = logger; }

    public void ResetCredentialStatus() { _authFailed = false; Status = "Credential updated; run the enrichment task"; }

    public async Task RunAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        await _run.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var config = Plugin.Instance?.Configuration;
            var token = _evidence.ReadToken();
            if (config?.EnableTmdb != true || token is null) { Status = "Offline mode; TMDB is disabled or has no credential"; return; }
            if (_authFailed) { Status = "TMDB rejected the credential; update it before retrying"; return; }
            var now = DateTimeOffset.UtcNow;
            var candidates = _index.Snapshot.Items.Values.Where(i => i.WorkKey.Contains(":tmdb:", StringComparison.Ordinal)).GroupBy(i => i.WorkKey).Select(g => g.First())
                .Where(i => _evidence.Snapshot.GetValueOrDefault(i.WorkKey)?.ProviderFetchedAt is not { } at || at < now.AddDays(-7))
                .OrderBy(i => _evidence.Snapshot.GetValueOrDefault(i.WorkKey)?.ProviderFetchedAt ?? DateTimeOffset.MinValue).Take(config.EnrichmentBatchSize).ToArray();
            for (var n = 0; n < candidates.Length; n++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = candidates[n];
                Status = $"Enriching {n + 1} of {candidates.Length} local works";
                try
                {
                    var id = item.WorkKey.Split(':')[2];
                    var type = item.Kind == MediaKind.Movie ? "movie" : "tv";
                    using var response = await FetchAsync($"{type}/{id}?append_to_response=keywords&language=en-US", token, cancellationToken).ConfigureAwait(false);
                    if (response is null) { if (_authFailed) break; continue; }
                    var root = response.RootElement;
                    var keywords = root.TryGetProperty("keywords", out var kw) ? (kw.TryGetProperty("keywords", out var movieKw) ? movieKw : kw.TryGetProperty("results", out var tvKw) ? tvKw : default) : default;
                    var keywordNames = keywords.ValueKind == JsonValueKind.Array ? keywords.EnumerateArray().Select(k => k.GetProperty("name").GetString() ?? "").Where(s => s.Length > 0).Take(500).ToArray() : [];
                    var evidence = new WorkEvidence { WorkKey = item.WorkKey, Rating = new() { Provider = "tmdb", Value = root.GetProperty("vote_average").GetDouble(), Scale = 10,
                        Votes = root.GetProperty("vote_count").GetInt64(), FetchedAt = DateTimeOffset.UtcNow }, Popularity = root.GetProperty("popularity").GetDouble(),
                        ProviderFetchedAt = DateTimeOffset.UtcNow, PopularityProvider = "tmdb", Keywords = keywordNames };
                    if (item.Kind == MediaKind.Series) evidence = evidence with { Manifest = await ManifestAsync(item.WorkKey, id, root, token, cancellationToken).ConfigureAwait(false) };
                    _evidence.UpdateProvider(evidence);
                }
                catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidDataException or KeyNotFoundException or IOException)
                { _logger.LogWarning("TMDB enrichment failed for {Work}: {Type}; prior evidence retained", item.WorkKey, ex.GetType().Name); }
                progress.Report(100.0 * (n + 1) / Math.Max(1, candidates.Length));
            }
            _recommendations.Invalidate();
            if (!_authFailed) Status = $"Enrichment finished; examined {candidates.Length} local works";
        }
        finally { _run.Release(); }
    }

    private async Task<EpisodeManifest?> ManifestAsync(string key, string id, JsonElement root, string token, CancellationToken cancellationToken)
    {
        var seasons = root.GetProperty("seasons").EnumerateArray().Where(s => s.GetProperty("season_number").GetInt32() > 0).ToArray();
        if (seasons.Length == 0 || seasons.Length > 50) return null;
        var episodes = new List<ManifestEpisode>();
        foreach (var season in seasons)
        {
            var number = season.GetProperty("season_number").GetInt32();
            using var response = await FetchAsync($"tv/{id}/season/{number}?language=en-US", token, cancellationToken).ConfigureAwait(false);
            if (response is null) return null;
            foreach (var ep in response.RootElement.GetProperty("episodes").EnumerateArray())
            {
                DateTimeOffset? air = ep.TryGetProperty("air_date", out var date) && DateTimeOffset.TryParse(date.GetString(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;
                episodes.Add(new() { Season = number, Number = ep.GetProperty("episode_number").GetInt32(), ProviderId = ep.GetProperty("id").GetInt64().ToString(System.Globalization.CultureInfo.InvariantCulture), AirDate = air });
            }
        }
        return new() { WorkKey = key, Source = "tmdb", Order = "aired", Finished = root.GetProperty("status").GetString() is "Ended" or "Canceled",
            Complete = episodes.Count > 0 && episodes.Count == root.GetProperty("number_of_episodes").GetInt32() && seasons.Length == root.GetProperty("number_of_seasons").GetInt32(),
            FetchedAt = DateTimeOffset.UtcNow, Episodes = episodes.ToArray() };
    }

    internal async Task<JsonDocument?> FetchAsync(string route, string token, CancellationToken cancellationToken)
    {
        var client = _clients.CreateClient("DiscoveryRowsTmdb");
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var delay = TimeSpan.FromMilliseconds(500) - (DateTimeOffset.UtcNow - _lastRequest);
            if (delay > TimeSpan.Zero) await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            using var request = new HttpRequestMessage(HttpMethod.Get, route);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _lastRequest = DateTimeOffset.UtcNow; RequestCount++;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            HttpResponseMessage response;
            try { response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { continue; }
            using (response)
            {
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) { _authFailed = true; Status = "TMDB rejected the credential; update it before retrying"; return null; }
                if (response.StatusCode == HttpStatusCode.NotFound) return null;
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
                        return await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { continue; }
                }
                if (response.StatusCode != HttpStatusCode.TooManyRequests && (int)response.StatusCode < 500) return null;
                if (attempt < 2)
                {
                    var backoff = response.Headers.RetryAfter?.Delta ?? (response.Headers.RetryAfter?.Date is { } retry ? retry - DateTimeOffset.UtcNow : TimeSpan.FromSeconds(Math.Pow(2, attempt) + Random.Shared.NextDouble()));
                    await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(backoff.TotalSeconds, .5, 30)), cancellationToken).ConfigureAwait(false);
                }
            }
        }
        return null;
    }
}
