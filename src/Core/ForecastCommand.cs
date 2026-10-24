using System.CommandLine;

namespace Core;

/// <summary>The command tree of the console program: <c>add</c> and <c>list</c>.</summary>
public static class ForecastCommand
{
    private const string DefaultDatabase = "forecast.db";

    /// <summary>The root command; <paramref name="http"/> carries every request to the geocoding service.</summary>
    public static RootCommand Create(HttpClient http)
    {
        var geocoding = new GeocodingClient(http);

        var addName = new Argument<string>("name") { Description = "The name of the place to track." };
        var add = new Command("add", "Adds a place to the tracked locations.");
        add.Arguments.Add(addName);
        Option<string> addDatabase = DatabaseOption();
        add.Options.Add(addDatabase);
        add.SetAction(async (result, cancellationToken) =>
        {
            string name = result.GetRequiredValue(addName);
            GeocodingResult found = await geocoding.FindAsync(name, cancellationToken).ConfigureAwait(false);
            TextWriter error = result.InvocationConfiguration.Error;
            switch (found.Status)
            {
                case GeocodingStatus.Found:
                    var store = new LocationStore(result.GetRequiredValue(addDatabase));
                    Location saved = await store.AddAsync(found.Place!, cancellationToken).ConfigureAwait(false);
                    await result.InvocationConfiguration.Output.WriteLineAsync($"Added {saved.Describe()}").ConfigureAwait(false);
                    return 0;
                case GeocodingStatus.NoMatch:
                    await error.WriteLineAsync($"No place has the name {name}.").ConfigureAwait(false);
                    return 1;
                case GeocodingStatus.SeveralMatches:
                    await error.WriteLineAsync($"The name {name} matches several places.").ConfigureAwait(false);
                    return 1;
                case GeocodingStatus.NoAnswer:
                default:
                    await error.WriteLineAsync("The geocoding service gave no answer.").ConfigureAwait(false);
                    return 1;
            }
        });

        var list = new Command("list", "Prints the tracked locations.");
        Option<string> listDatabase = DatabaseOption();
        list.Options.Add(listDatabase);
        list.SetAction(async (result, cancellationToken) =>
        {
            var store = new LocationStore(result.GetRequiredValue(listDatabase));
            IReadOnlyList<Location> places = await store.ListAsync(cancellationToken).ConfigureAwait(false);
            TextWriter output = result.InvocationConfiguration.Output;
            if (places.Count is 0)
            {
                await output.WriteLineAsync("No tracked locations.").ConfigureAwait(false);
            }

            foreach (Location place in places)
            {
                await output.WriteLineAsync(place.ToString()).ConfigureAwait(false);
            }

            return 0;
        });

        var root = new RootCommand("Tracks the places whose weather you follow.");
        root.Subcommands.Add(add);
        root.Subcommands.Add(list);
        return root;
    }

    private static Option<string> DatabaseOption() => new("--database")
    {
        Description = "The SQLite file that holds the tracked locations.",
        DefaultValueFactory = _ => DefaultDatabase,
    };
}
