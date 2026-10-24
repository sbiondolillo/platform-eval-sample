using System.Globalization;
using System.Text.Json;

namespace Core;

/// <summary>The service gave no usable answer: it was unreachable, timed out or answered with an error status.</summary>
public sealed class GeocodingUnavailableException : Exception
{
    public GeocodingUnavailableException()
    {
    }

    public GeocodingUnavailableException(string message)
        : base(message)
    {
    }

    public GeocodingUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Asks the geocoding service of Open-Meteo for the places of a name.</summary>
public sealed class GeocodingClient(HttpClient http)
{
    private const string SearchUrl = "https://geocoding-api.open-meteo.com/v1/search";

    public async Task<IReadOnlyList<Place>> SearchAsync(string name, CancellationToken cancellationToken)
    {
        var uri = new Uri($"{SearchUrl}?name={Uri.EscapeDataString(name)}");
        try
        {
            using HttpResponseMessage response = await http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return Parse(body);
        }
        catch (HttpRequestException e)
        {
            throw new GeocodingUnavailableException("The service gave no answer.", e);
        }
        catch (TaskCanceledException e) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GeocodingUnavailableException("The service gave no answer.", e);
        }
        catch (JsonException e)
        {
            throw new GeocodingUnavailableException("The service gave an answer that is not readable.", e);
        }
    }

    /// <summary>The places in the <c>results</c> of an answer; an answer with no <c>results</c> holds none.</summary>
    public static IReadOnlyList<Place> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind is not JsonValueKind.Object
            || !document.RootElement.TryGetProperty("results", out JsonElement results)
            || results.ValueKind is not JsonValueKind.Array)
        {
            return [];
        }

        return [.. results.EnumerateArray().Select(r => new Place(
            r.GetProperty("name").GetString() ?? "",
            r.GetProperty("latitude").GetDouble(),
            r.GetProperty("longitude").GetDouble()))];
    }

    public static string Format(string name, double latitude, double longitude) =>
        string.Create(CultureInfo.InvariantCulture, $"{name} ({latitude}, {longitude})");
}
