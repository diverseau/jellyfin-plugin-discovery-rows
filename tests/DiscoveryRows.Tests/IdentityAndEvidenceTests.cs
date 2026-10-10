using System.Security.Claims;
using DiscoveryRows.Core;
using Jellyfin.Plugin.DiscoveryRows.Configuration;
using Jellyfin.Plugin.DiscoveryRows.Services;
using Xunit;

namespace DiscoveryRows.Tests;

public sealed class IdentityAndEvidenceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid User = Guid.Parse("51783739-4dda-4521-9597-79c0944d988c");
    private static ClaimsPrincipal Principal(string? id, bool authenticated = true) => new(new ClaimsIdentity(id is null ? [] : [new Claim("Jellyfin-UserId", id)], authenticated ? "Test" : null));
    [Fact] public void AuthenticatedClaimMustMatchPayloadExactly()
    {
        Assert.True(RequestIdentity.Matches(Principal(User.ToString()), User));
        Assert.False(RequestIdentity.Matches(Principal(User.ToString()), Guid.NewGuid()));
        Assert.False(RequestIdentity.Matches(Principal(User.ToString()), Guid.Empty));
    }
    [Fact] public void UnauthenticatedMissingInvalidAndApiKeyClaimsFail()
    {
        Assert.False(RequestIdentity.Matches(Principal(User.ToString(), false), User));
        Assert.False(RequestIdentity.Matches(Principal(null), User));
        Assert.False(RequestIdentity.Matches(Principal("bad-guid"), User));
        Assert.False(RequestIdentity.Matches(null, User));
        Assert.False(RequestIdentity.Matches(new ClaimsPrincipal(new ClaimsIdentity([new Claim("Jellyfin-Token", "api-key")], "Test")), User));
    }
    [Fact] public void HssReflectionCreatesTheExpectedJObjectType()
    {
        var expected = typeof(Newtonsoft.Json.Linq.JObject);
        var payload = HssBridge.ParseJObject(expected, "{\"id\":\"DiscoveryRowsUnder90Movies\"}");
        Assert.IsType<Newtonsoft.Json.Linq.JObject>(payload);
        Assert.Equal("DiscoveryRowsUnder90Movies", ((Newtonsoft.Json.Linq.JObject)payload).Value<string>("id"));
    }
    [Fact] public void EveryShelfHasStableCssSafeIdAndMatchingWrapper()
    {
        Assert.Equal(38, RowCatalogue.All.Count);
        Assert.Equal(38, RowCatalogue.All.Select(r => r.SectionId).Distinct().Count());
        foreach (var r in RowCatalogue.All)
        {
            Assert.Matches("^[A-Za-z][A-Za-z0-9]*$", r.SectionId);
            var method = typeof(HssResultsHandler).GetMethod("GetRow_" + r.Key);
            Assert.NotNull(method);
            Assert.Equal(typeof(MediaBrowser.Model.Querying.QueryResult<MediaBrowser.Model.Dto.BaseItemDto>), method.ReturnType);
        }
    }
    private static EvidenceDocument Document(WorkEvidence? w = null) => new() { Items = [w ?? new() { WorkKey = "movie:tmdb:12" }] };
    [Theory] [InlineData("movie:tmdb:12")] [InlineData("series:tmdb:12")] [InlineData("movie:imdb:tt123456")]
    [InlineData("movie:jellyfin:517837394dda4521959779c0944d988c")]
    public void ExplicitSupportedWorkKeysValidate(string key) => Assert.Empty(EvidenceValidator.Validate(Document(new() { WorkKey = key }), Now));
    [Theory] [InlineData("actor:tmdb:12")] [InlineData("movie:tmdb:-1")] [InlineData("movie:tmdb:0")] [InlineData("../foo")]
    public void AmbiguousAndPersonKeysFail(string key) => Assert.NotEmpty(EvidenceValidator.Validate(Document(new() { WorkKey = key }), Now));
    [Fact] public void UnknownSchemaAndDuplicateKeysFail()
    {
        Assert.NotEmpty(EvidenceValidator.Validate(Document() with { SchemaVersion = 2 }, Now));
        Assert.NotEmpty(EvidenceValidator.Validate(Document() with { Items = [Document().Items[0], Document().Items[0]] }, Now));
    }
    [Fact] public void ReviewedAssertionsNeedProvenanceAndReviewTime()
    {
        var w = new WorkEvidence { WorkKey = "movie:tmdb:12", Facets = [new() { Name = "uplifting", Confidence = .9, Reviewed = true }] };
        Assert.NotEmpty(EvidenceValidator.Validate(Document(w), Now));
        Assert.Empty(EvidenceValidator.Validate(Document(w with { Facets = [w.Facets[0] with { ReviewedAt = Now }] }), Now));
    }
    [Fact] public void NonfiniteInvalidConfidenceAndRatingsFail()
    {
        var w = new WorkEvidence { WorkKey = "movie:tmdb:12", Rating = new() { Value = double.NaN, Votes = 100, FetchedAt = Now } };
        Assert.NotEmpty(EvidenceValidator.Validate(Document(w), Now));
        Assert.NotEmpty(EvidenceValidator.Validate(Document(w with { Rating = w.Rating with { Value = 11 } }), Now));
        Assert.NotEmpty(EvidenceValidator.Validate(Document(w with { Rating = w.Rating with { Value = 8, Votes = -5 } }), Now));
        Assert.NotEmpty(EvidenceValidator.Validate(Document(w with { Rating = null, Facets = [new() { Name = "uplifting", Confidence = 1.5 }] }), Now));
    }
    [Fact] public void ImportedAwardsMustBindTheParentWorkAndValidOutcome()
    {
        var a = new AwardRecord { WorkKey = "movie:tmdb:12", Body = "academy", Category = "Picture", CeremonyYear = 2025, Outcome = "winner", EvidenceUrl = "https://www.oscars.org/oscars/ceremonies/2025" };
        var w = new WorkEvidence { WorkKey = a.WorkKey, Awards = [a] };
        Assert.Empty(EvidenceValidator.Validate(Document(w), Now));
        Assert.NotEmpty(EvidenceValidator.Validate(Document(w with { Awards = [a with { WorkKey = "movie:tmdb:99" }] }), Now));
        Assert.NotEmpty(EvidenceValidator.Validate(Document(w with { Awards = [a with { Outcome = "nominated-ish" }] }), Now));
        Assert.NotEmpty(EvidenceValidator.Validate(Document(w with { Awards = [a, a with { Outcome = "nominee" }] }), Now));
    }
    [Fact] public void ManifestCannotBeAttachedToMovieOrDifferentSeries()
    {
        var manifest = new EpisodeManifest { WorkKey = "series:tmdb:12", FetchedAt = Now, Episodes = [new() { Season = 1, Number = 1, AirDate = Now.AddDays(-1) }] };
        Assert.Empty(EvidenceValidator.Validate(Document(new() { WorkKey = manifest.WorkKey, Manifest = manifest }), Now));
        Assert.NotEmpty(EvidenceValidator.Validate(Document(new() { WorkKey = "movie:tmdb:12", Manifest = manifest }), Now));
        Assert.NotEmpty(EvidenceValidator.Validate(Document(new() { WorkKey = "series:tmdb:13", Manifest = manifest }), Now));
    }
    [Fact] public void ConfigurationRejectsInvalidSizeAndUnknownRows()
    {
        Assert.Empty(new PluginConfiguration().Validate());
        Assert.NotEmpty(new PluginConfiguration { Rows = [new() { Key = "under90", Limit = 0 }] }.Validate());
        Assert.NotEmpty(new PluginConfiguration { Rows = [new() { Key = "under90", Limit = 3, Minimum = 6 }] }.Validate());
        Assert.NotEmpty(new PluginConfiguration { Rows = [new() { Key = "random" }] }.Validate());
        Assert.NotEmpty(new PluginConfiguration { MaximumCachedUsers = 0 }.Validate());
    }
}
