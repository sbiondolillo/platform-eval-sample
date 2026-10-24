using Core;
using System.Net;
using Xunit;

namespace Specs.Unit;

public sealed class GeocodingClientTests
{
    [Fact]
    public void ParseReadsNameAndPositionOfEachResult()
    {
        IReadOnlyList<Place> places = GeocodingClient.Parse("""{"results":[{"id":1,"name":"Kathmandu","latitude":27.70169,"longitude":85.3206}]}""");

        Assert.Equal([new Place("Kathmandu", 27.70169, 85.3206)], places);
    }

    [Fact]
    public void ParseReturnsNoPlaceWhenResultsIsMissing() =>
        Assert.Empty(GeocodingClient.Parse("""{"generationtime_ms":0.4}"""));

    [Fact]
    public void FormatUsesTheInvariantCulture() =>
        Assert.Equal("Vaduz (47.14151, 9.52154)", GeocodingClient.Format("Vaduz", 47.14151, 9.52154));

    [Fact]
    public async Task SearchEscapesTheNameInTheRequest()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        var client = new GeocodingClient(new HttpClient(handler));

        await client.SearchAsync("Sao Paulo", CancellationToken.None);

        Assert.Equal("https://geocoding-api.open-meteo.com/v1/search?name=Sao%20Paulo", handler.Uri?.AbsoluteUri);
    }

    [Fact]
    public async Task SearchTreatsAnErrorStatusAsNoAnswer()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = new GeocodingClient(new HttpClient(handler));

        await Assert.ThrowsAsync<GeocodingUnavailableException>(() => client.SearchAsync("Kathmandu", CancellationToken.None));
    }

    [Fact]
    public async Task SearchTreatsATimeoutAsNoAnswer()
    {
        var handler = new FakeHandler(_ => throw new TaskCanceledException());
        var client = new GeocodingClient(new HttpClient(handler));

        await Assert.ThrowsAsync<GeocodingUnavailableException>(() => client.SearchAsync("Kathmandu", CancellationToken.None));
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public Uri? Uri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri = request.RequestUri;
            return Task.FromResult(answer(request));
        }
    }
}
