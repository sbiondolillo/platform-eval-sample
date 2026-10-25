using Core;
using Xunit;

namespace Specs.Unit;

public sealed class LocationStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"store-{Guid.NewGuid():N}.db");

    public void Dispose() => File.Delete(_path);

    [Fact]
    public async Task ListOfAMissingFileIsEmptyAndCreatesNothing()
    {
        Assert.Empty(await new LocationStore(_path).ListAsync(CancellationToken.None));
        Assert.False(File.Exists(_path));
    }

    [Fact]
    public async Task AddGivesIdsInOrderAndListReturnsThem()
    {
        var store = new LocationStore(_path);

        TrackedLocation first = await store.AddAsync(new Place("Kathmandu", 27.70169, 85.3206), CancellationToken.None);
        TrackedLocation second = await store.AddAsync(new Place("Vaduz", 47.14151, 9.52154), CancellationToken.None);

        Assert.Equal([1L, 2L], [first.Id, second.Id]);
        Assert.Equal([first, second], await store.ListAsync(CancellationToken.None));
    }
}
