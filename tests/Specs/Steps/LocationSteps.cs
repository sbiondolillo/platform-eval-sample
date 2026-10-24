using Reqnroll;
using Core;
using System.CommandLine;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Specs.Steps;

[Binding]
public sealed class LocationSteps : IDisposable
{
    private readonly Dictionary<string, string> _answers = [];
    private readonly List<string> _requests = [];
    private readonly string _directory = Directory.CreateTempSubdirectory("forecast-").FullName;
    private bool _noAnswer;
    private string _output = "";
    private string _error = "";
    private int _exitCode = -1;
    private string _kept = "";

    private string Database => Path.Combine(_directory, "forecast.db");

    [Given("the service answers {string} with one place {string} at {double}, {double}")]
    public void GivenOnePlace(string query, string name, double latitude, double longitude) =>
        _answers[query] = JsonSerializer.Serialize(new { results = new[] { new { id = 1, name, latitude, longitude, country = "Nowhere" } }, generationtime_ms = 0.2 });

    [Given("the service answers {string} with no place")]
    public void GivenNoPlace(string query) => _answers[query] = """{"generationtime_ms":0.4}""";

    [Given("the service answers {string} with several places")]
    public void GivenSeveralPlaces(string query) =>
        _answers[query] = JsonSerializer.Serialize(new { results = new[] { new { id = 1, name = query, latitude = 42.36, longitude = -71.06 }, new { id = 2, name = query, latitude = 52.97, longitude = -0.02 } } });

    [Given("the service gives no answer")]
    public void GivenNoAnswer() => _noAnswer = true;

    [Given("the service answers or fails for {string}")]
    public void GivenAnswersOrFails(string name) => _noAnswer = !_answers.ContainsKey(name);

    [Given("I ran {string} on a database file")]
    [When("I run {string} on a database file")]
    public async Task RunOnDatabase(string command)
    {
        List<string> args = [.. command.Split(' ')];
        if (args.Count > 1 || args[0] is "list")
        {
            args.AddRange(["--database", Database]);
        }

        await Run(args).ConfigureAwait(false);
    }

    [When("I run {string}")]
    public Task RunPlain(string command) => Run([.. command.Split(' ')]);

    [Given("the output was kept")]
    public void GivenTheOutputWasKept() => _kept = _output;

    [Then("the program sent the request {string}")]
    public void ThenSent(string request) => Assert.Equal([request], _requests);

    [Then("the program sent no request")]
    public void ThenSentNone() => Assert.Empty(_requests);

    [Then("standard output is {string}")]
    public void ThenOutputIs(string expected) => Assert.Equal(expected, _output.TrimEnd());

    [Then("standard output is two lines")]
    public void ThenTwoLines(DataTable table) =>
        Assert.Equal(table.Rows.Select(row => row["line"].Trim()), _output.TrimEnd().Split('\n').Select(line => line.Trim()));

    [Then("standard output is empty")]
    public void ThenOutputEmpty() => Assert.Equal("", _output);

    [Then("standard output holds {string}")]
    public void ThenOutputHolds(string text) => Assert.Contains(text, _output, StringComparison.Ordinal);

    [Then("standard error says no place has the name {string}")]
    public void ThenNoPlace(string name)
    {
        Assert.Contains(name, _error, StringComparison.Ordinal);
        Assert.Contains("No place has the name", _error, StringComparison.Ordinal);
    }

    [Then("standard error says the name {string} matches several places")]
    public void ThenSeveral(string name)
    {
        Assert.Contains(name, _error, StringComparison.Ordinal);
        Assert.Contains("matches several places", _error, StringComparison.Ordinal);
    }

    [Then("standard error says the service gave no answer")]
    public void ThenServiceNoAnswer() => Assert.Contains("gave no answer", _error, StringComparison.Ordinal);

    [Then("standard error is not empty")]
    public void ThenErrorNotEmpty() => Assert.NotEmpty(_error);

    [Then("the exit code is {int}")]
    public void ThenExitCode(int code) => Assert.Equal(code, _exitCode);

    [Then("the exit code is not {int}")]
    public void ThenExitCodeNot(int code) => Assert.NotEqual(code, _exitCode);

    [Then("{string} on the database file prints the kept output")]
    public async Task ThenListUnchanged(string command)
    {
        _noAnswer = false;
        await RunOnDatabase(command).ConfigureAwait(false);
        Assert.Equal(_kept, _output);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, true);
        GC.SuppressFinalize(this);
    }

    private async Task Run(List<string> args)
    {
        using var http = new HttpClient(new StubHandler(this));
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        var configuration = new InvocationConfiguration { Output = output, Error = error };
        _exitCode = await ForecastCommand.Create(http).Parse([.. args]).InvokeAsync(configuration).ConfigureAwait(false);
        _output = output.ToString();
        _error = error.ToString();
    }

    private sealed class StubHandler(LocationSteps steps) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            steps._requests.Add($"{request.Method} {request.RequestUri}");
            string query = Uri.UnescapeDataString(request.RequestUri!.Query.Replace("?name=", "", StringComparison.Ordinal));
            if (steps._noAnswer || !steps._answers.TryGetValue(query, out string? body))
            {
                throw new HttpRequestException("no answer");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}
