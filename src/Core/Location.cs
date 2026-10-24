using System.Globalization;

namespace Core;

/// <summary>A tracked place: its id, name and coordinates.</summary>
public sealed record Location(long Id, string Name, double Latitude, double Longitude)
{
    /// <summary>The place as the program prints it: <c>Name (latitude, longitude)</c>.</summary>
    public string Describe() => FormattableString.Invariant($"{Name} ({Latitude}, {Longitude})");

    /// <inheritdoc />
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Id} {Describe()}");
}
