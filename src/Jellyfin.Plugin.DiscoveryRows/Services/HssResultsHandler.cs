using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.DiscoveryRows.Services;

public sealed class HssRequest
{
    public Guid UserId { get; set; }
    public string? AdditionalData { get; set; }
}

public sealed class HssResultsHandler(RecommendationService recommendations, IHttpContextAccessor context)
{
    private QueryResult<BaseItemDto> Get(string row, HssRequest request)
    {
        if (!RequestIdentity.Matches(context.HttpContext?.User, request.UserId)) return new(Array.Empty<BaseItemDto>());
        return recommendations.Results(request.UserId, row, enforceSelection: true);
    }
    public QueryResult<BaseItemDto> GetRow_under90(HssRequest request) => Get("under90", request);
    public QueryResult<BaseItemDto> GetRow_comedy90(HssRequest request) => Get("comedy90", request);
    public QueryResult<BaseItemDto> GetRow_thriller90(HssRequest request) => Get("thriller90", request);
    public QueryResult<BaseItemDto> GetRow_shortepisodes(HssRequest request) => Get("shortepisodes", request);
    public QueryResult<BaseItemDto> GetRow_laughs30(HssRequest request) => Get("laughs30", request);
    public QueryResult<BaseItemDto> GetRow_shortdocs(HssRequest request) => Get("shortdocs", request);
    public QueryResult<BaseItemDto> GetRow_laughmovies(HssRequest request) => Get("laughmovies", request);
    public QueryResult<BaseItemDto> GetRow_popcorn(HssRequest request) => Get("popcorn", request);
    public QueryResult<BaseItemDto> GetRow_feelgood(HssRequest request) => Get("feelgood", request);
    public QueryResult<BaseItemDto> GetRow_casual(HssRequest request) => Get("casual", request);
    public QueryResult<BaseItemDto> GetRow_crowd(HssRequest request) => Get("crowd", request);
    public QueryResult<BaseItemDto> GetRow_quality(HssRequest request) => Get("quality", request);
    public QueryResult<BaseItemDto> GetRow_hidden(HssRequest request) => Get("hidden", request);
    public QueryResult<BaseItemDto> GetRow_gems(HssRequest request) => Get("gems", request);
    public QueryResult<BaseItemDto> GetRow_opening(HssRequest request) => Get("opening", request);
    public QueryResult<BaseItemDto> GetRow_globes(HssRequest request) => Get("globes", request);
    public QueryResult<BaseItemDto> GetRow_oscars(HssRequest request) => Get("oscars", request);
    public QueryResult<BaseItemDto> GetRow_emmys(HssRequest request) => Get("emmys", request);
    public QueryResult<BaseItemDto> GetRow_onenight(HssRequest request) => Get("onenight", request);
    public QueryResult<BaseItemDto> GetRow_weekend(HssRequest request) => Get("weekend", request);
    public QueryResult<BaseItemDto> GetRow_complete(HssRequest request) => Get("complete", request);
    public QueryResult<BaseItemDto> GetRow_firstseason(HssRequest request) => Get("firstseason", request);
    public QueryResult<BaseItemDto> GetRow_chosenfamily(HssRequest request) => Get("chosenfamily", request);
    public QueryResult<BaseItemDto> GetRow_cyberpunk(HssRequest request) => Get("cyberpunk", request);
    public QueryResult<BaseItemDto> GetRow_dystopia(HssRequest request) => Get("dystopia", request);
    public QueryResult<BaseItemDto> GetRow_food(HssRequest request) => Get("food", request);
    public QueryResult<BaseItemDto> GetRow_smalltown(HssRequest request) => Get("smalltown", request);
    public QueryResult<BaseItemDto> GetRow_smalltownscares(HssRequest request) => Get("smalltownscares", request);
    public QueryResult<BaseItemDto> GetRow_scams(HssRequest request) => Get("scams", request);
    public QueryResult<BaseItemDto> GetRow_arthorror(HssRequest request) => Get("arthorror", request);
    public QueryResult<BaseItemDto> GetRow_gentlereality(HssRequest request) => Get("gentlereality", request);
    public QueryResult<BaseItemDto> GetRow_witchcraft(HssRequest request) => Get("witchcraft", request);
    public QueryResult<BaseItemDto> GetRow_books(HssRequest request) => Get("books", request);
    public QueryResult<BaseItemDto> GetRow_truestory(HssRequest request) => Get("truestory", request);
    public QueryResult<BaseItemDto> GetRow_family(HssRequest request) => Get("family", request);
    public QueryResult<BaseItemDto> GetRow_nostalgia80(HssRequest request) => Get("nostalgia80", request);
    public QueryResult<BaseItemDto> GetRow_twistedchristmas(HssRequest request) => Get("twistedchristmas", request);
    public QueryResult<BaseItemDto> GetRow_cringe(HssRequest request) => Get("cringe", request);
}
