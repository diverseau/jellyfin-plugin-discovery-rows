namespace DiscoveryRows.Core;

public static class RowAllocator
{
    public static IReadOnlyDictionary<string, RankedCandidate[]> Allocate(IReadOnlyList<(RowSettings Settings, RankedCandidate[] Candidates)> rows, bool deduplicate)
    {
        // Remove undersized rows before assigning, then restart if allocation makes another row too small.
        var active = rows.Where(r => r.Candidates.Length >= r.Settings.Minimum).ToList();
        while (true)
        {
            var result = new Dictionary<string, RankedCandidate[]>(StringComparer.Ordinal);
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in active.OrderBy(r => r.Candidates.Length).ThenBy(r => r.Settings.Key, StringComparer.Ordinal))
            {
                var franchises = new Dictionary<string, int>(StringComparer.Ordinal);
                var selected = new List<RankedCandidate>();
                foreach (var candidate in row.Candidates)
                {
                    if (deduplicate && used.Contains(candidate.Item.WorkKey)) continue;
                    var franchise = candidate.Item.FranchiseKey;
                    if (franchise is not null && franchises.GetValueOrDefault(franchise) >= row.Settings.FranchiseCap) continue;
                    selected.Add(candidate);
                    if (franchise is not null) franchises[franchise] = franchises.GetValueOrDefault(franchise) + 1;
                    if (selected.Count == row.Settings.Limit) break;
                }
                // An undersized row must not reserve titles and cause later, useful shelves to fail too.
                if (selected.Count < row.Settings.Minimum) selected.Clear();
                if (deduplicate) used.UnionWith(selected.Select(x => x.Item.WorkKey));
                result[row.Settings.Key] = selected.ToArray();
            }
            var failed = active.Where(r => result[r.Settings.Key].Length < r.Settings.Minimum).ToArray();
            if (failed.Length == 0)
            {
                foreach (var row in rows) result.TryAdd(row.Settings.Key, []);
                return result;
            }
            foreach (var row in failed) active.Remove(row);
        }
    }
}
