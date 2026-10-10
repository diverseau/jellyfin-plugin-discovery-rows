using System.Collections.Immutable;
using System.Text.Json;
using DiscoveryRows.Core;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DiscoveryRows.Services;

public sealed class EvidenceStore
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _folder;
    private readonly object _gate = new();
    private readonly ILogger<EvidenceStore> _logger;
    private ImmutableDictionary<string, WorkEvidence> _items = ImmutableDictionary<string, WorkEvidence>.Empty;
    private long _revision;
    public string? LoadError { get; private set; }
    public long Revision => Interlocked.Read(ref _revision);
    public IReadOnlyDictionary<string, WorkEvidence> Snapshot => _items;
    public string DataFolder => _folder;

    public EvidenceStore(IApplicationPaths paths, ILogger<EvidenceStore> logger)
    {
        _folder = Path.Combine(paths.DataPath, "discovery-rows");
        _logger = logger;
        Directory.CreateDirectory(_folder);
        var file = Path.Combine(_folder, "evidence.json");
        if (!File.Exists(file)) return;
        try
        {
            var document = JsonSerializer.Deserialize<EvidenceDocument>(File.ReadAllText(file), Json) ?? throw new InvalidDataException("Empty evidence document.");
            var errors = EvidenceValidator.Validate(document, DateTimeOffset.UtcNow);
            if (errors.Count > 0) throw new InvalidDataException(string.Join("; ", errors.Take(5)));
            _items = document.Items.ToImmutableDictionary(w => w.WorkKey, StringComparer.Ordinal);
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or ArgumentException)
        {
            LoadError = "Evidence could not be loaded. The original file is preserved; repair or import valid evidence.";
            _logger.LogError(ex, "Discovery Rows evidence load failed; original file preserved");
        }
    }

    public void Import(EvidenceDocument document)
    {
        var errors = EvidenceValidator.Validate(document, DateTimeOffset.UtcNow);
        if (errors.Count > 0) throw new ArgumentException(string.Join("; ", errors.Take(20)), nameof(document));
        lock (_gate)
        {
            var next = _items.SetItems(document.Items.Select(w => new KeyValuePair<string, WorkEvidence>(w.WorkKey, w)));
            Persist(next);
            _items = next;
            LoadError = null;
            Interlocked.Increment(ref _revision);
        }
    }

    public void UpdateProvider(WorkEvidence provider)
    {
        lock (_gate)
        {
            var current = _items.GetValueOrDefault(provider.WorkKey) ?? new WorkEvidence { WorkKey = provider.WorkKey };
            // Enrichment never overwrites reviewed facets, exclusions or work-linked awards.
            var merged = current with { Rating = current.Rating is { Provider: not "tmdb" } ? current.Rating : provider.Rating,
                Popularity = provider.Popularity, PopularityProvider = provider.PopularityProvider, ProviderFetchedAt = provider.ProviderFetchedAt, Keywords = provider.Keywords,
                Manifest = current.Manifest is { Source: not "tmdb" } ? current.Manifest : provider.Manifest ?? current.Manifest };
            var errors = EvidenceValidator.Validate(new() { Items = [merged] }, DateTimeOffset.UtcNow);
            if (errors.Count > 0) throw new InvalidDataException(string.Join("; ", errors));
            var next = _items.SetItem(provider.WorkKey, merged);
            Persist(next);
            _items = next;
            Interlocked.Increment(ref _revision);
        }
    }

    private void Persist(ImmutableDictionary<string, WorkEvidence> next)
    {
        if (LoadError is not null)
        {
            var file = Path.Combine(_folder, "evidence.json");
            if (File.Exists(file)) File.Copy(file, file + ".recovery-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
        }
        AtomicWrite(Path.Combine(_folder, "evidence.json"), JsonSerializer.Serialize(new EvidenceDocument { Items = next.Values.OrderBy(w => w.WorkKey, StringComparer.Ordinal).ToArray() }, Json));
    }

    public static void AtomicWrite(string file, string text, bool secret = false)
    {
        var temporary = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                if (secret && !OperatingSystem.IsWindows()) File.SetUnixFileMode(temporary, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                using var writer = new StreamWriter(stream);
                writer.Write(text); writer.Flush(); stream.Flush(true);
            }
            File.Move(temporary, file, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public bool HasToken => File.Exists(Path.Combine(_folder, "tmdb-token"));
    public string? ReadToken() => HasToken ? File.ReadAllText(Path.Combine(_folder, "tmdb-token")).Trim() : null;
    public void SetToken(string? token)
    {
        lock (_gate)
        {
            var file = Path.Combine(_folder, "tmdb-token");
            if (string.IsNullOrWhiteSpace(token)) { File.Delete(file); return; }
            if (token.Length > 4096 || token.Any(char.IsWhiteSpace)) throw new ArgumentException("Invalid TMDB token.");
            AtomicWrite(file, token, true);
        }
    }
}
