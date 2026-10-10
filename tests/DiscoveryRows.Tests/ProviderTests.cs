using System.Net;
using System.Net.Http.Headers;
using Jellyfin.Plugin.DiscoveryRows.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DiscoveryRows.Tests;

public sealed class FakeProviderHandler(Func<int, HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response) : HttpMessageHandler
{
    public int Requests { get; private set; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => response(++Requests, request, cancellationToken);
}
public sealed class FakeClientFactory(HttpClient client) : IHttpClientFactory { public HttpClient CreateClient(string name) => client; }

public sealed class ProviderTests
{
    private static TmdbEnrichmentService Service(HttpClient client) => new(new FakeClientFactory(client), null!, null!, null!, NullLogger<TmdbEnrichmentService>.Instance);
    private static HttpClient Client(HttpMessageHandler handler) => new(handler) { BaseAddress = new Uri("https://provider.invalid/3/") };
    [Fact] public async Task AuthenticationFailureDoesNotRetryOrExposeTokenInStatus()
    {
        using var handler = new FakeProviderHandler((_, request, _) =>
        {
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme); Assert.Equal("fixture-secret", request.Headers.Authorization?.Parameter);
            Assert.DoesNotContain("fixture-secret", request.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        });
        using var client = Client(handler); var service = Service(client);
        Assert.Null(await service.FetchAsync("movie/1?append_to_response=keywords", "fixture-secret", CancellationToken.None));
        Assert.Equal(1, handler.Requests); Assert.Contains("rejected", service.Status); Assert.DoesNotContain("fixture-secret", service.Status);
    }
    [Fact] public async Task RateLimitHonorsBoundedRetryAndReturnsSuccessfulEvidence()
    {
        using var handler = new FakeProviderHandler((n, _, _) =>
        {
            var response = new HttpResponseMessage(n == 1 ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);
            response.Content = new StringContent("{\"vote_average\":8.2,\"vote_count\":10000}"); return Task.FromResult(response);
        });
        using var client = Client(handler); using var json = await Service(client).FetchAsync("movie/1", "fixture", CancellationToken.None);
        Assert.Equal(2, handler.Requests); Assert.Equal(10000, json!.RootElement.GetProperty("vote_count").GetInt32());
    }
    [Fact] public async Task RepeatedProviderFailureStopsAfterThreeAttempts()
    {
        using var handler = new FakeProviderHandler((_, _, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable); response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero); return Task.FromResult(response);
        });
        using var client = Client(handler); Assert.Null(await Service(client).FetchAsync("movie/1", "fixture", CancellationToken.None)); Assert.Equal(3, handler.Requests);
    }
    [Fact] public async Task MissingProviderRecordDoesNotRetry()
    {
        using var handler = new FakeProviderHandler((_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = Client(handler); Assert.Null(await Service(client).FetchAsync("movie/1", "fixture", CancellationToken.None)); Assert.Equal(1, handler.Requests);
    }
    [Fact] public async Task ShutdownCancellationStopsNetworkWork()
    {
        using var handler = new FakeProviderHandler(async (_, _, cancellation) => { await Task.Delay(TimeSpan.FromMinutes(1), cancellation); return new HttpResponseMessage(HttpStatusCode.OK); });
        using var client = Client(handler); using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service(client).FetchAsync("movie/1", "fixture", cancel.Token)); Assert.Equal(1, handler.Requests);
    }
}
