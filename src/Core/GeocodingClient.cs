using System.Text.Json;

namespace Core;

/// <summary>The answer of the geocoding service to a name.</summary>
public enum GeocodingStatus
{
    Found,
    NoMatch,
    SeveralMatches,
    NoAnswer,
}

/// <summary>The status of a lookup and, when it is <see cref="GeocodingStatus.Found"/>, the one place.</summary>
public sealed record GeocodingResult(GeocodingStatus Status, Location? Place);

/// <summary>Asks the geocoding service of Open-Meteo for the places of a name.</summary>
public sealed class GeocodingClient(HttpClient http)
{
    private const string SearchUrl = "https://geocoding-api.open-meteo.com/v1/search";

    public async Task<GeocodingResult> FindAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{SearchUrl}?name={Uri.EscapeDataString(name)}");
            using HttpResponseMessage response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("results", out JsonElement results) || results.GetArrayLength() is 0)
            {
                return new GeocodingResult(GeocodingStatus.NoMatch, null);
            }

            if (results.GetArrayLength() > 1)
            {
                return new GeocodingResult(GeocodingStatus.SeveralMatches, null);
            }

            JsonElement match = results[0];
            var place = new Location(0, match.GetProperty("name").GetString() ?? name, match.GetProperty("latitude").GetDouble(), match.GetProperty("longitude").GetDouble());
            return new GeocodingResult(GeocodingStatus.Found, place);
        }
        catch (HttpRequestException)
        {
            return new GeocodingResult(GeocodingStatus.NoAnswer, null);
        }
        catch (JsonException)
        {
            return new GeocodingResult(GeocodingStatus.NoAnswer, null);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new GeocodingResult(GeocodingStatus.NoAnswer, null);
        }
    }
}
