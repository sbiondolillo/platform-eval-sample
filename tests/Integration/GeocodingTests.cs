using Core;
using Xunit;

namespace Integration;

public sealed class GeocodingTests
{
    private static async Task<IReadOnlyList<Place>> SearchAsync(string name)
    {
        using var http = new HttpClient();
        return await new GeocodingClient(http).SearchAsync(name, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("Kathmandu", 27.70169, 85.3206)]
    [InlineData("Vaduz", 47.14151, 9.52154)]
    public async Task ANameWithOnePlaceGivesOneResult(string name, double latitude, double longitude) =>
        Assert.Equal([new Place(name, latitude, longitude)], await SearchAsync(name));

    [Fact]
    public async Task BostonGivesSeveralResults() => Assert.True((await SearchAsync("Boston")).Count > 1);

    [Fact]
    public async Task ANameThatNoPlaceHasGivesNoResults() => Assert.Empty(await SearchAsync("Zzzzqqqxxx"));
}
