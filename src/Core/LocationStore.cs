using Microsoft.Data.Sqlite;

namespace Core;

/// <summary>The tracked locations, in a SQLite database file.</summary>
public sealed class LocationStore(string path)
{
    private SqliteConnection Open(SqliteOpenMode mode)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = mode,
            Pooling = false,
        }.ToString());
        connection.Open();
        return connection;
    }

    public async Task<TrackedLocation> AddAsync(Place place, CancellationToken cancellationToken)
    {
        using SqliteConnection connection = Open(SqliteOpenMode.ReadWriteCreate);
        using (SqliteCommand create = connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE IF NOT EXISTS locations (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, latitude REAL NOT NULL, longitude REAL NOT NULL)";
            await create.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        using SqliteCommand insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO locations (name, latitude, longitude) VALUES ($name, $latitude, $longitude); SELECT last_insert_rowid()";
        insert.Parameters.AddWithValue("$name", place.Name);
        insert.Parameters.AddWithValue("$latitude", place.Latitude);
        insert.Parameters.AddWithValue("$longitude", place.Longitude);
        object? id = await insert.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return new TrackedLocation(Convert.ToInt64(id, System.Globalization.CultureInfo.InvariantCulture), place.Name, place.Latitude, place.Longitude);
    }

    /// <summary>The tracked locations in id order; none when the file does not exist.</summary>
    public async Task<IReadOnlyList<TrackedLocation>> ListAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        using SqliteConnection connection = Open(SqliteOpenMode.ReadOnly);
        using SqliteCommand select = connection.CreateCommand();
        select.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'locations'";
        if (await select.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is null)
        {
            return [];
        }

        select.CommandText = "SELECT id, name, latitude, longitude FROM locations ORDER BY id";
        var locations = new List<TrackedLocation>();
        using SqliteDataReader reader = await select.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            locations.Add(new TrackedLocation(reader.GetInt64(0), reader.GetString(1), reader.GetDouble(2), reader.GetDouble(3)));
        }

        return locations;
    }
}
