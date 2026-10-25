namespace Core;

/// <summary>A place that the geocoding service found: a name and a position.</summary>
public sealed record Place(string Name, double Latitude, double Longitude);

/// <summary>A place that a person tracks, with the id that the store gave it.</summary>
public sealed record TrackedLocation(long Id, string Name, double Latitude, double Longitude);
