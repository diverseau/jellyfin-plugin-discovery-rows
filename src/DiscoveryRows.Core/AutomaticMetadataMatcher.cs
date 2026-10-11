namespace DiscoveryRows.Core;

public sealed record MetadataMatch(string Reason, double Strength = .8);

/// <summary>Explainable catalogue matching. These matches are estimates, never fabricated reviewed records.</summary>
public static class AutomaticMetadataMatcher
{
    public static bool Supports(string? facet) => facet is "humour" or "family_intent" or "uplifting" or "popcorn" or "broad_appeal"
        or "low_cognitive_load" or "episodic" or "found_family" or "cyberpunk" or "future_dystopia" or "food_central"
        or "small_town_charm" or "small_town_scary" or "scams_central" or "art_horror" or "gentle_british_reality"
        or "witchcraft_central" or "book_adaptation" or "dramatized_true_story" or "1980s_nostalgia" or "twisted_christmas"
        or "cringe_comedy" or "complete_limited_story";

    private static string Normalize(string value) => string.Join(' ', new string(value.ToLowerInvariant()
        .Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray()).Split(' ', StringSplitOptions.RemoveEmptyEntries));

    public static MetadataMatch? Match(string facet, MediaCandidate item, WorkEvidence? evidence)
    {
        if (!Supports(facet)) return null;
        var genres = (item.Genres.Length > 0 ? item.Genres : evidence?.Metadata?.Genres ?? []).Select(Normalize).ToHashSet(StringComparer.Ordinal);
        var tags = item.Tags.Select(Normalize).ToHashSet(StringComparer.Ordinal);
        var keywords = (evidence?.Keywords ?? []).Select(Normalize).ToHashSet(StringComparer.Ordinal);
        bool Genre(params string[] values) => values.Any(v => genres.Contains(Normalize(v)));
        string? Term(params string[] values) => values.Select(Normalize).FirstOrDefault(v => tags.Contains(v) || keywords.Contains(v));
        MetadataMatch? Named(params string[] values)
        {
            var term = Term(values);
            return term is null ? null : new("Automatic metadata: " + (tags.Contains(term) ? "Jellyfin tag '" : "catalogue keyword '") + term + "'");
        }
        MetadataMatch By(string reason) => new("Automatic metadata: " + reason);
        var dark = Genre("Horror", "Thriller", "Crime", "War") || Term("dark comedy", "black comedy", "tragedy", "tragic ending", "suicide", "serial killer") is not null;
        var mature = Normalize(item.OfficialRating ?? "") is "r" or "rated r" or "us r" or "nc 17" or "tv ma" or "ma15" or "ma15 plus" or "r18" or "r18 plus" or "18" or "gb 18" or "au ma15" or "au r18";
        var comedy = Genre("Comedy");
        var town = Term("small town", "small town life", "small town community", "village") is not null;
        var year = item.ProductionYear ?? evidence?.Metadata?.Year;
        switch (facet)
        {
            case "humour": return comedy ? By("Comedy genre") : Named("humour", "humor", "stand up comedy", "slapstick");
            case "family_intent":
                if (item.Kind != MediaKind.Movie || dark || mature) return null;
                return Genre("Family") ? By("Family genre, without dark genres or a known mature rating")
                    : Genre("Animation") && Normalize(item.OfficialRating ?? "") is "g" or "pg" or "u" ? By("Animation genre and all-ages/PG rating")
                    : Named("family_intent", "family film", "family movie", "children's film", "kids movie");
            case "uplifting":
                if (dark) return null;
                return Named("uplifting", "feel good", "heartwarming", "inspirational")
                    ?? (comedy && Genre("Family", "Romance", "Animation") ? By("Comedy with Family, Romance or Animation; dark genres excluded") : null);
            case "popcorn": return Named("popcorn", "blockbuster") ?? (Genre("Action", "Adventure", "Science Fiction") ? By("Action, Adventure or Science Fiction genre") : null);
            case "broad_appeal": return Named("broad_appeal", "blockbuster") ?? (Genre("Action", "Adventure", "Comedy", "Family") ? By("mainstream genre; audience rating is checked separately") : null);
            case "low_cognitive_load":
            case "episodic":
                if (item.Kind != MediaKind.Series || dark) return null;
                return Named(facet, "sitcom", "episodic", "sketch comedy")
                    ?? (comedy && item.Episodes.Any(e => e.Season > 0) && item.Episodes.Where(e => e.Season > 0).All(e => RowEvaluator.Fits(e.Runtimes, 35))
                        ? By("short Comedy episodes; estimated casual viewing") : null);
            case "found_family": return Named("found_family", "found family", "chosen family", "surrogate family");
            case "cyberpunk": return Named("cyberpunk") ?? (Genre("Science Fiction") ? Named("cyberspace", "virtual reality", "hacker", "cybernetics") : null);
            case "future_dystopia": return Named("future_dystopia", "dystopia", "dystopian future", "post apocalyptic");
            case "food_central": return Named("food_central", "cooking", "chef", "cuisine", "baking", "restaurant", "culinary arts");
            case "small_town_charm": return town && !dark && Genre("Comedy", "Romance", "Family") ? By("small-town keyword with Comedy, Romance or Family genre") : Named("small_town_charm");
            case "small_town_scary": return town && Genre("Horror", "Thriller") ? By("small-town keyword with Horror or Thriller genre") : Named("small_town_scary");
            case "scams_central": return Named("scams_central", "con artist", "confidence trick", "scam", "swindler", "fraud");
            case "art_horror": return Genre("Horror") ? Named("art_horror", "arthouse", "psychological horror", "surrealism", "elevated horror", "slow burn") : null;
            case "gentle_british_reality":
                var british = (evidence?.Metadata?.Countries ?? []).Select(Normalize).Any(c => c is "gb" or "uk" or "united kingdom");
                return british && Genre("Reality", "Reality TV") && !dark
                    ? Named("baking", "gardening", "crafts", "cooking competition", "gentle_british_reality") : null;
            case "witchcraft_central": return Named("witchcraft_central", "witchcraft", "witch", "witches", "black magic", "occult");
            case "book_adaptation": return Named("book_adaptation", "based on novel", "based on a novel", "based on novel or book", "based on book", "literary adaptation", "based on children's book", "based on graphic novel");
            case "dramatized_true_story": return Genre("Documentary") ? null : Named("dramatized_true_story", "based on true story", "based on a true story", "true story");
            case "1980s_nostalgia": return year is >= 1980 and <= 1989 ? By("release year " + year) : Named("1980s_nostalgia", "1980s nostalgia");
            case "twisted_christmas": return Term("christmas", "christmas horror") is not null && (dark || Genre("Horror", "Crime", "Thriller"))
                ? By("Christmas keyword with dark genre or dark-comedy keyword") : Named("twisted_christmas");
            case "cringe_comedy": return comedy ? Named("cringe_comedy", "cringe comedy", "social awkwardness", "humiliation", "mockumentary") : null;
            case "complete_limited_story": return item.Kind == MediaKind.Series && evidence?.Metadata is { SeriesType: "Miniseries", Status: "Ended" }
                ? By("TMDB identifies an ended miniseries; every required local episode is checked separately") : null;
            default: return null;
        }
    }
}
