using Core;
using Xunit;

namespace Integration;

public class GeocodingTests
{
    private static async Task<GeocodingResult> Find(string name)
    {
        using var http = new HttpClient();
        return await new GeocodingClient(http).FindAsync(name, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task OnePlaceHasTheNameKathmandu()
    {
        GeocodingResult result = await Find("Kathmandu");
        Assert.Equal(GeocodingStatus.Found, result.Status);
        Assert.Equal("Kathmandu", result.Place!.Name);
        Assert.Equal(27.70169, result.Place.Latitude);
        Assert.Equal(85.3206, result.Place.Longitude);
    }

    [Fact]
    public async Task OnePlaceHasTheNameVaduz()
    {
        GeocodingResult result = await Find("Vaduz");
        Assert.Equal(GeocodingStatus.Found, result.Status);
        Assert.Equal(47.14151, result.Place!.Latitude);
        Assert.Equal(9.52154, result.Place.Longitude);
    }

    [Fact]
    public async Task SeveralPlacesHaveTheNameBoston() => Assert.Equal(GeocodingStatus.SeveralMatches, (await Find("Boston")).Status);

    [Fact]
    public async Task NoPlaceHasTheNameZzzzqqqxxx() => Assert.Equal(GeocodingStatus.NoMatch, (await Find("Zzzzqqqxxx")).Status);
}
