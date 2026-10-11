namespace DiscoveryRows.Core;

public static class RowCatalogue
{
    public static readonly IReadOnlyList<RowDefinition> All = Build().ToArray();
    public static RowDefinition? Find(string key) => All.FirstOrDefault(x => x.Key == key);

    private static IEnumerable<RowDefinition> Build()
    {
        yield return new("under90", "DiscoveryRowsUnder90Movies", "Movies Under 90 Minutes", MediaKind.Movie, SelectorKind.Runtime, 90, true);
        yield return new("comedy90", "DiscoveryRows90MinuteComedies", "90-Minute Comedies", MediaKind.Movie, SelectorKind.Runtime, 90, Genre: "Comedy");
        yield return new("thriller90", "DiscoveryRows90MinuteThrillers", "90-Minute Thrillers", MediaKind.Movie, SelectorKind.Runtime, 90, Genre: "Thriller");
        yield return new("shortepisodes", "DiscoveryRowsShortEpisodes", "Short Episodes, 30 Minutes or Less", MediaKind.Series, SelectorKind.ShortEpisodes, 30);
        yield return new("laughs30", "DiscoveryRows30MinuteLaughs", "30-Minute Laughs", MediaKind.Series, SelectorKind.ShortEpisodes, 30, Genre: "Comedy");
        yield return new("shortdocs", "DiscoveryRowsShortDocumentaries", "Short Documentaries", MediaKind.Movie, SelectorKind.Runtime, 60, Genre: "Documentary");
        yield return new("laughmovies", "DiscoveryRowsLaughMovies", "Movies That Make You Laugh", MediaKind.Movie, SelectorKind.Facet, Facet: "humour");
        yield return new("popcorn", "DiscoveryRowsPopcornMovies", "Perfect Popcorn Movies", MediaKind.Movie, SelectorKind.Facet, Facet: "popcorn");
        yield return new("feelgood", "DiscoveryRowsFeelGoodMovies", "Feel-Good Movies", MediaKind.Movie, SelectorKind.Facet, Facet: "uplifting", Confidence: .8);
        yield return new("casual", "DiscoveryRowsCasualViewing", "Casual Viewing", MediaKind.Series, SelectorKind.Facet, Facet: "low_cognitive_load", Confidence: .8);
        yield return new("crowd", "DiscoveryRowsCrowdPleasers", "Crowd Pleasers", MediaKind.Movie, SelectorKind.Facet, Facet: "broad_appeal");
        yield return new("quality", "DiscoveryRowsHighlyRatedMovies", "Highly Rated Movies", MediaKind.Movie, SelectorKind.Quality);
        yield return new("hidden", "DiscoveryRowsHiddenLibrary", "Hidden in Your Library", null, SelectorKind.Hidden);
        yield return new("gems", "DiscoveryRowsGemsForYou", "Gems for You", MediaKind.Movie, SelectorKind.Gems);
        yield return new("opening", "DiscoveryRowsStrongOpening", "Hooked from the First Episode", MediaKind.Series, SelectorKind.Facet, Facet: "strong_opening", Confidence: .85);
        yield return new("globes", "DiscoveryRowsGoldenGlobes", "Golden Globe Nominees", null, SelectorKind.Award, AwardBody: "golden_globes");
        yield return new("oscars", "DiscoveryRowsOscars", "Oscar Winners", MediaKind.Movie, SelectorKind.Award, AwardBody: "academy", WinnerOnly: true);
        yield return new("emmys", "DiscoveryRowsEmmys", "Emmy-Winning TV", MediaKind.Series, SelectorKind.Award, AwardBody: "emmy", WinnerOnly: true);
        yield return new("onenight", "DiscoveryRowsOneNight", "Watch in One Night", MediaKind.Series, SelectorKind.CompleteStory, 240);
        yield return new("weekend", "DiscoveryRowsOneWeekend", "Watch in One Weekend", MediaKind.Series, SelectorKind.CompleteStory, 720);
        yield return new("complete", "DiscoveryRowsCompleteSeries", "Complete Series, Ready to Watch", MediaKind.Series, SelectorKind.CompleteSeries);
        yield return new("firstseason", "DiscoveryRowsFirstSeasons", "Great First Seasons", MediaKind.Series, SelectorKind.FirstSeason, Facet: "great_first_season", Confidence: .85);
        var themes = new (string Key, string Title, string Facet)[]
        {
            ("chosenfamily", "Chosen Family", "found_family"), ("cyberpunk", "Cyberpunk", "cyberpunk"),
            ("dystopia", "Dystopian Futures", "future_dystopia"), ("food", "Don't Watch Hungry", "food_central"),
            ("smalltown", "Small Town Charm", "small_town_charm"), ("smalltownscares", "Small Town Scares", "small_town_scary"),
            ("scams", "Schemers & Scammers", "scams_central"), ("arthorror", "High Brow Horror", "art_horror"),
            ("gentlereality", "Gentle British Reality TV", "gentle_british_reality"), ("witchcraft", "Witchcraft & the Dark Arts", "witchcraft_central"),
            ("books", "Based on Books", "book_adaptation"), ("truestory", "Based on a True Story", "dramatized_true_story"),
            ("family", "Family Movie Night", "family_intent"), ("nostalgia80", "Totally Awesome '80s", "1980s_nostalgia"),
            ("twistedchristmas", "Twisted Christmas", "twisted_christmas"), ("cringe", "Cringe Comedy", "cringe_comedy")
        };
        foreach (var t in themes) yield return new(t.Key, "DiscoveryRowsTheme" + t.Key, t.Title, t.Key == "family" ? MediaKind.Movie : null, SelectorKind.Theme, Facet: t.Facet, Confidence: .8);
    }

    public static RowSettings Defaults(RowDefinition row) => new()
    {
        Key = row.Key,
        Enabled = All.Take(15).Contains(row),
        MinimumVotes = row.Key == "crowd" ? 500 : 100,
        QualityMinimum = row.Key == "crowd" ? 6.5 : 7
    };
}
