using DiscoveryRows.Core;
using Jellyfin.Plugin.DiscoveryRows.Configuration;
using Jellyfin.Plugin.DiscoveryRows.Services;
using Jellyfin.Plugin.DiscoveryRows.Tasks;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.DiscoveryRows.Controllers;

public sealed record TokenRequest(string? Token);
public sealed record FeedbackRequest(Guid ItemId, bool Hidden);
public sealed record ExclusionRequest(string WorkKey, bool Excluded);
public sealed record RestoreRequest(string WorkKey);

[ApiController]
[Route("DiscoveryRows")]
[Authorize]
public sealed class DiscoveryRowsController(LibrarySnapshotService index, EvidenceStore evidence, RecommendationService recommendations,
    HssBridge hss, TmdbEnrichmentService enrichment, UserHistoryStore history, IUserManager users, ILibraryManager library, ITaskManager tasks) : ControllerBase
{
    [HttpGet("Status")]
    [Authorize(Roles = "Administrator")]
    public object Status() => new { pluginVersion = "1.0.0", jellyfinTarget = "10.11.11", hssTarget = "3.0.2.0", hssStatus = hss.Status,
        registeredRows = hss.RegisteredCount, labelsReady = hss.LabelsReady, layoutAvailable = hss.LayoutAvailable, preferencesPageReady = hss.PreferencesPageReady,
        indexRevision = index.Snapshot.Revision, indexedTitles = index.Snapshot.Items.Count, indexedAt = index.Snapshot.At, indexDirty = index.Dirty,
        indexError = index.LastError, evidenceRecords = evidence.Snapshot.Count, evidenceError = evidence.LoadError,
        historyError = history.LoadError, hasTmdbToken = evidence.HasToken, enrichmentStatus = enrichment.Status, providerRequests = enrichment.RequestCount,
        cachedUsers = recommendations.CachedUsers, requests = recommendations.Requests, lastHandlerMilliseconds = recommendations.LastMilliseconds };

    [HttpGet("tmdb-logo.svg")]
    [AllowAnonymous]
    public IActionResult Logo()
    {
        var stream = typeof(Plugin).Assembly.GetManifestResourceStream("Jellyfin.Plugin.DiscoveryRows.Configuration.tmdb-logo.svg");
        return stream is null ? NotFound() : File(stream, "image/svg+xml");
    }

    [HttpGet("Rows")]
    [Authorize(Roles = "Administrator")]
    public object Rows() => RowCatalogue.All.Select(r => new { definition = r, settings = Plugin.Instance!.Configuration.GetRow(r) }).ToArray();

    [HttpGet("Users")]
    [Authorize(Roles = "Administrator")]
    public object Users() => users.GetUsers().Select(u => new { id = u.Id, name = u.Username }).ToArray();

    [HttpPut("Configuration")]
    [Authorize(Roles = "Administrator")]
    public IActionResult Configuration([FromBody] PluginConfiguration config)
    {
        var errors = config.Validate();
        if (errors.Count > 0) return BadRequest(new { errors });
        Plugin.Instance!.UpdateConfiguration(config); index.MarkDirty(); return NoContent();
    }

    [HttpPost("Refresh")]
    [Authorize(Roles = "Administrator")]
    public IActionResult Refresh() { tasks.QueueIfNotRunning<RefreshTask>(); return Accepted(); }

    [HttpPost("Enrich")]
    [Authorize(Roles = "Administrator")]
    public IActionResult Enrich() { tasks.QueueIfNotRunning<EnrichmentTask>(); return Accepted(); }

    [HttpPost("Credential")]
    [Authorize(Roles = "Administrator")]
    [RequestSizeLimit(8192)]
    public IActionResult Credential([FromBody] TokenRequest request)
    {
        try { evidence.SetToken(request.Token); enrichment.ResetCredentialStatus(); return NoContent(); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("Coverage")]
    [Authorize(Roles = "Administrator")]
    public IActionResult Coverage([FromQuery] Guid userId, [FromQuery] string? row = null)
    {
        if (row is not null && RowCatalogue.Find(row) is null) return BadRequest(new { error = "Unknown row." });
        try { return Ok(recommendations.Coverage(userId, row)); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("Preview/{row}")]
    [Authorize(Roles = "Administrator")]
    public ActionResult<QueryResult<BaseItemDto>> Preview(string row, [FromQuery] Guid userId)
    {
        if (RowCatalogue.Find(row) is null || recommendations.GetUser(userId) is null) return BadRequest();
        return recommendations.Results(userId, row, false);
    }

    [HttpGet("Library")]
    [Authorize(Roles = "Administrator")]
    public IActionResult Library([FromQuery] Guid userId, [FromQuery] string? search = null, [FromQuery] int start = 0, [FromQuery] int limit = 100)
    {
        var user = recommendations.GetUser(userId);
        if (user is null) return BadRequest();
        var media = index.ForUser(user, Plugin.Instance!.Configuration).Where(m => string.IsNullOrWhiteSpace(search) || m.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).OrderBy(m => m.Name, StringComparer.Ordinal).ToArray();
        return Ok(new { total = media.Length, items = media.Skip(Math.Max(0, start)).Take(Math.Clamp(limit, 1, 200)).Select(m => new { m.Id, m.WorkKey, m.Name, m.Kind, m.Genres, m.Started, evidence = evidence.Snapshot.GetValueOrDefault(m.WorkKey) }) });
    }

    [HttpGet("Evidence")]
    [Authorize(Roles = "Administrator")]
    public EvidenceDocument Export() => new() { Items = evidence.Snapshot.Values.OrderBy(w => w.WorkKey, StringComparer.Ordinal).ToArray() };

    [HttpPut("Evidence")]
    [Authorize(Roles = "Administrator")]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public IActionResult Import([FromBody] EvidenceDocument document)
    {
        var errors = EvidenceValidator.Validate(document, DateTimeOffset.UtcNow);
        if (errors.Count > 0) return BadRequest(new { errors });
        evidence.Import(document); recommendations.Invalidate(); return Ok(new { imported = document.Items.Length, total = evidence.Snapshot.Count });
    }

    [HttpPost("Exclusion")]
    [Authorize(Roles = "Administrator")]
    public IActionResult Exclusion([FromBody] ExclusionRequest request)
    {
        if (!EvidenceValidator.IsWorkKey(request.WorkKey)) return BadRequest();
        var work = evidence.Snapshot.GetValueOrDefault(request.WorkKey) ?? new WorkEvidence { WorkKey = request.WorkKey };
        evidence.Import(new() { Items = [work with { Excluded = request.Excluded }] }); recommendations.Invalidate(); return NoContent();
    }

    [HttpGet("Preferences")]
    public IActionResult Preferences()
    {
        var id = RequestIdentity.UserId(User); var user = recommendations.GetUser(id);
        if (user is null) return Forbid();
        var visible = index.ForUser(user, Plugin.Instance!.Configuration).GroupBy(m => m.WorkKey).ToDictionary(g => g.Key, g => g.First());
        return Ok(history.GetFeedback(id).Where(f => visible.ContainsKey(f.WorkKey)).Select(f => new { f.WorkKey, f.Hidden, name = visible[f.WorkKey].Name, itemId = visible[f.WorkKey].Id }).ToArray());
    }

    [HttpPost("Preferences")]
    public IActionResult Preferences([FromBody] FeedbackRequest request)
    {
        var id = RequestIdentity.UserId(User); var user = recommendations.GetUser(id);
        if (user is null) return Forbid();
        var item = library.GetItemById(request.ItemId);
        if (item is not (Movie or Series) || !item.IsVisibleStandalone(user)) return Forbid();
        var key = LibrarySnapshotService.WorkKey(item, item is Movie ? MediaKind.Movie : MediaKind.Series);
        history.SetFeedback(id, key, request.Hidden); recommendations.Invalidate(); return NoContent();
    }

    [HttpGet("Preferences/Library")]
    public IActionResult PreferenceLibrary([FromQuery] string? search = null)
    {
        var user = recommendations.GetUser(RequestIdentity.UserId(User));
        if (user is null) return Forbid();
        var hidden = history.Hidden(user.Id);
        return Ok(index.ForUser(user, Plugin.Instance!.Configuration).Where(m => string.IsNullOrWhiteSpace(search) || m.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).OrderBy(m => m.Name, StringComparer.Ordinal).Take(100).Select(m => new { m.Id, m.Name, m.WorkKey, hidden = hidden.Contains(m.WorkKey) }).ToArray());
    }

    [HttpPost("Preferences/Restore")]
    public IActionResult Restore([FromBody] RestoreRequest request)
    {
        var user = recommendations.GetUser(RequestIdentity.UserId(User));
        if (user is null) return Forbid();
        if (!index.ForUser(user, Plugin.Instance!.Configuration).Any(m => m.WorkKey == request.WorkKey)) return Forbid();
        history.SetFeedback(user.Id, request.WorkKey, false); recommendations.Invalidate(); return NoContent();
    }
}
