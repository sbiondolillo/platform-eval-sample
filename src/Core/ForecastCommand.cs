using System.CommandLine;

namespace Core;

/// <summary>The command tree of the console program: <c>add</c> and <c>list</c>, each with <c>--database</c>.</summary>
public static class ForecastCommand
{
    private const string DefaultDatabase = "forecast.db";

    /// <summary>The root command; <paramref name="http"/> carries every request to the geocoding service.</summary>
    public static RootCommand Create(HttpClient http)
    {
        var root = new RootCommand("Tracks the places whose weather a person follows.");
        root.Subcommands.Add(CreateAdd(new GeocodingClient(http)));
        root.Subcommands.Add(CreateList());
        return root;
    }

    private static Option<string> DatabaseOption() => new("--database")
    {
        Description = "The SQLite database file that holds the tracked locations.",
        DefaultValueFactory = _ => DefaultDatabase,
    };

    private static Command CreateAdd(GeocodingClient geocoding)
    {
        var name = new Argument<string>("name") { Description = "The name of the place to track." };
        Option<string> database = DatabaseOption();
        var add = new Command("add", "Adds the place of a name to the tracked locations.");
        add.Arguments.Add(name);
        add.Options.Add(database);
        add.SetAction(async (result, cancellationToken) =>
        {
            string placeName = result.GetRequiredValue(name);
            TextWriter error = result.InvocationConfiguration.Error;
            IReadOnlyList<Place> places;
            try
            {
                places = await geocoding.SearchAsync(placeName, cancellationToken).ConfigureAwait(false);
            }
            catch (GeocodingUnavailableException e)
            {
                await error.WriteLineAsync($"The geocoding service gave no answer for {placeName}: {e.Message}").ConfigureAwait(false);
                return 1;
            }

            if (places.Count is 0)
            {
                await error.WriteLineAsync($"No place has the name {placeName}.").ConfigureAwait(false);
                return 1;
            }

            if (places.Count > 1)
            {
                await error.WriteLineAsync($"The name {placeName} matches several places.").ConfigureAwait(false);
                return 1;
            }

            Place place = places[0];
            await new LocationStore(result.GetRequiredValue(database)).AddAsync(place, cancellationToken).ConfigureAwait(false);
            await result.InvocationConfiguration.Output.WriteLineAsync($"Added {GeocodingClient.Format(place.Name, place.Latitude, place.Longitude)}").ConfigureAwait(false);
            return 0;
        });
        return add;
    }

    private static Command CreateList()
    {
        Option<string> database = DatabaseOption();
        var list = new Command("list", "Prints the tracked locations.");
        list.Options.Add(database);
        list.SetAction(async (result, cancellationToken) =>
        {
            IReadOnlyList<TrackedLocation> locations = await new LocationStore(result.GetRequiredValue(database)).ListAsync(cancellationToken).ConfigureAwait(false);
            TextWriter output = result.InvocationConfiguration.Output;
            if (locations.Count is 0)
            {
                await output.WriteLineAsync("No tracked locations.").ConfigureAwait(false);
            }

            foreach (TrackedLocation location in locations)
            {
                await output.WriteLineAsync($"{location.Id} {GeocodingClient.Format(location.Name, location.Latitude, location.Longitude)}").ConfigureAwait(false);
            }

            return 0;
        });
        return list;
    }
}
