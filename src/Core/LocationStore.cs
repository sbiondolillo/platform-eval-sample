using Microsoft.Data.Sqlite;

namespace Core;

/// <summary>The tracked locations, in a SQLite database file.</summary>
public sealed class LocationStore(string path)
{
    private SqliteConnection Open(SqliteOpenMode mode) =>
        new(new SqliteConnectionStringBuilder { DataSource = path, Mode = mode, Pooling = false }.ToString());

    /// <summary>Stores a place and returns it with its id. The file and its table are made on the first call.</summary>
    public async Task<Location> AddAsync(Location place, CancellationToken cancellationToken)
    {
        SqliteConnection connection = Open(SqliteOpenMode.ReadWriteCreate);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            SqliteCommand create = connection.CreateCommand();
            await using (create.ConfigureAwait(false))
            {
                create.CommandText = "CREATE TABLE IF NOT EXISTS locations (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, latitude REAL NOT NULL, longitude REAL NOT NULL)";
                await create.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            SqliteCommand insert = connection.CreateCommand();
            await using (insert.ConfigureAwait(false))
            {
                insert.CommandText = "INSERT INTO locations (name, latitude, longitude) VALUES ($name, $latitude, $longitude) RETURNING id";
                insert.Parameters.AddWithValue("$name", place.Name);
                insert.Parameters.AddWithValue("$latitude", place.Latitude);
                insert.Parameters.AddWithValue("$longitude", place.Longitude);
                object? id = await insert.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return place with { Id = Convert.ToInt64(id, System.Globalization.CultureInfo.InvariantCulture) };
            }
        }
    }

    /// <summary>The tracked locations in the order they were added; empty when no file exists.</summary>
    public async Task<IReadOnlyList<Location>> ListAsync(CancellationToken cancellationToken)
    {
        var places = new List<Location>();
        if (!File.Exists(path))
        {
            return places;
        }

        SqliteConnection connection = Open(SqliteOpenMode.ReadOnly);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            SqliteCommand select = connection.CreateCommand();
            await using (select.ConfigureAwait(false))
            {
                select.CommandText = "SELECT id, name, latitude, longitude FROM locations ORDER BY id";
                SqliteDataReader reader = await select.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        places.Add(new Location(reader.GetInt64(0), reader.GetString(1), reader.GetDouble(2), reader.GetDouble(3)));
                    }
                }
            }
        }

        return places;
    }
}
