using System.Text.Json;

namespace Jellyfin.Plugin.DiscoveryRows.Services;

public sealed record Delivery(Guid UserId, string WorkKey, string Bucket, DateTimeOffset At);
public sealed record Feedback(Guid UserId, string WorkKey, bool Hidden, DateTimeOffset At);
public sealed record UserHistoryDocument(int SchemaVersion, Delivery[] Deliveries, Feedback[] Feedback);

public sealed class UserHistoryStore
{
    private readonly object _gate = new();
    private readonly string _file;
    private UserHistoryDocument _document = new(1, [], []);
    public string? LoadError { get; private set; }
    public long Revision { get; private set; }

    public UserHistoryStore(EvidenceStore evidence)
    {
        _file = Path.Combine(evidence.DataFolder, "user-history.json");
        try
        {
            if (File.Exists(_file))
            {
                var d = JsonSerializer.Deserialize<UserHistoryDocument>(File.ReadAllText(_file), EvidenceStore.Json);
                if (d is null || d.SchemaVersion != 1 || d.Deliveries is null || d.Feedback is null) throw new InvalidDataException();
                _document = d;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException)
        { LoadError = "User history could not be loaded; original file preserved."; }
    }

    public HashSet<string> Recent(Guid user, DateTimeOffset now)
    {
        var bucket = now.UtcDateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        lock (_gate) return _document.Deliveries.Where(d => d.UserId == user && d.Bucket != bucket && d.At >= now.AddDays(-7)).Select(d => d.WorkKey).ToHashSet(StringComparer.Ordinal);
    }
    public HashSet<string> Hidden(Guid user) { lock (_gate) return _document.Feedback.Where(f => f.UserId == user && f.Hidden).Select(f => f.WorkKey).ToHashSet(StringComparer.Ordinal); }
    public Feedback[] GetFeedback(Guid user) { lock (_gate) return _document.Feedback.Where(f => f.UserId == user).ToArray(); }

    public void Record(Guid user, IEnumerable<string> works, DateTimeOffset now)
    {
        lock (_gate)
        {
            var bucket = now.UtcDateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var entries = _document.Deliveries.Where(d => d.At >= now.AddDays(-30)).ToList();
            var added = false;
            foreach (var work in works)
            {
                if (entries.Any(d => d.UserId == user && d.WorkKey == work && d.Bucket == bucket)) continue;
                entries.Add(new(user, work, bucket, now)); added = true;
            }
            if (!added) return;
            var next = _document with { Deliveries = entries.TakeLast(50000).ToArray() };
            Save(next); _document = next;
        }
    }

    public void SetFeedback(Guid user, string work, bool hidden)
    {
        if (!global::DiscoveryRows.Core.EvidenceValidator.IsWorkKey(work)) throw new ArgumentException("Invalid work key.");
        lock (_gate)
        {
            var next = _document with { Feedback = _document.Feedback.Where(f => f.UserId != user || f.WorkKey != work).Append(new(user, work, hidden, DateTimeOffset.UtcNow)).TakeLast(50000).ToArray() };
            Save(next); _document = next; Revision++;
        }
    }

    private void Save(UserHistoryDocument next)
    {
        if (LoadError is not null && File.Exists(_file)) File.Copy(_file, _file + ".recovery-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
        EvidenceStore.AtomicWrite(_file, JsonSerializer.Serialize(next, EvidenceStore.Json), true);
        LoadError = null;
    }
}
