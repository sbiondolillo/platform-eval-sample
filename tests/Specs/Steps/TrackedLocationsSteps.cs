using Core;
using Reqnroll;
using System.CommandLine;
using System.Globalization;
using System.Net;
using Xunit;

namespace Specs.Steps;

[Binding]
public sealed class TrackedLocationsSteps : IDisposable
{
    private readonly string _database = Path.Combine(Path.GetTempPath(), $"forecast-{Guid.NewGuid():N}.db");
    private readonly StubHandler _handler = new();
    private string _output = "";
    private string _error = "";
    private int _exitCode = -1;
    private IReadOnlyList<string> _listBefore = [];

    public void Dispose()
    {
        _handler.Dispose();
        File.Delete(_database);
    }

    [Given("the service answers one result named {string} at {double}, {double}")]
    public void GivenOneResult(string name, double latitude, double longitude) =>
        _handler.Body = ResultsBody(Result(name, latitude, longitude));

    [Given("the service answers no result")]
    public void GivenNoResult() => _handler.Body = """{"generationtime_ms":0.41663647}""";

    [Given("the service answers several results")]
    public void GivenSeveralResults() =>
        _handler.Body = ResultsBody(Result("Boston", 42.35843, -71.05977), Result("Boston", 52.97609, -0.02649));

    [Given("the request gets no answer from the service")]
    public void GivenNoAnswer() => _handler.Body = null;

    [Given("no file exists at the database path")]
    public void GivenNoFile() => Assert.False(File.Exists(_database));

    [Given("a person ran {string} on the database file")]
    public async Task GivenAPersonRan(string command)
    {
        await RunAsync(command, onDatabase: true);
        Assert.Equal(0, _exitCode);
        _listBefore = [.. (await new LocationStore(_database).ListAsync(CancellationToken.None)).Select(Line)];
    }

    [When("a person runs {string}")]
    public Task WhenARuns(string command) => RunAsync(command, onDatabase: false);

    [When("a person runs {string} on the database file")]
    public Task WhenARunsOnFile(string command) => RunAsync(command, onDatabase: true);

    [When("a person runs {string} with no name")]
    public Task WhenARunsWithNoName(string command) => RunAsync(command, onDatabase: false);

    [Then("the program sent the request {string}")]
    public void ThenSent(string request) => Assert.Equal([request], _handler.Requests);

    [Then("the program sent no request")]
    public void ThenSentNone() => Assert.Empty(_handler.Requests);

    [Then("standard output is {string}")]
    public void ThenOutputIs(string expected) => Assert.Equal(expected, _output.TrimEnd('\n'));

    [Then("standard output is two lines, {string} and {string}")]
    public void ThenOutputIsTwoLines(string first, string second) =>
        Assert.Equal([first, second], _output.TrimEnd('\n').Split('\n'));

    [Then("standard output is empty")]
    public void ThenOutputEmpty() => Assert.Equal("", _output);

    [Then("standard output holds the option {string}")]
    public void ThenOutputHoldsOption(string option) => Assert.Contains(option, _output, StringComparison.Ordinal);

    [Then("standard output holds the default value {string}")]
    public void ThenOutputHoldsDefault(string value) => Assert.Contains(value, _output, StringComparison.Ordinal);

    [Then("standard error holds a line that says no place has the name and holds {string}")]
    public void ThenErrorNoPlace(string name) => AssertErrorLine("No place has the name", name);

    [Then("standard error holds a line that says the name matches several places and holds {string}")]
    public void ThenErrorSeveral(string name) => AssertErrorLine("matches several places", name);

    [Then("standard error holds a line that says the service gave no answer")]
    public void ThenErrorNoAnswer() => AssertErrorLine("gave no answer", "");

    [Then("standard error holds an error")]
    public void ThenErrorHolds() => Assert.NotEqual("", _error.Trim());

    [Then("the exit code is {int}")]
    public void ThenExit(int code) => Assert.Equal(code, _exitCode);

    [Then("the exit code is not {int}")]
    public void ThenExitNot(int code) => Assert.NotEqual(code, _exitCode);

    [Then("{string} on the database file prints the same lines as before the add")]
    public async Task ThenSameLines(string command)
    {
        await RunAsync(command, onDatabase: true);
        Assert.Equal(_listBefore, _output.TrimEnd('\n').Split('\n'));
    }

    private void AssertErrorLine(string phrase, string name) =>
        Assert.Contains(_error.Split('\n'), l => l.Contains(phrase, StringComparison.Ordinal) && l.Contains(name, StringComparison.Ordinal));

    private async Task RunAsync(string command, bool onDatabase)
    {
        string[] args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (onDatabase)
        {
            args = [.. args, "--database", _database];
        }
        else if (args is ["add", _])
        {
            args = [.. args, "--database", _database];
        }

        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);
        using var http = new HttpClient(_handler, disposeHandler: false);
        RootCommand root = ForecastCommand.Create(http);
        _exitCode = await root.Parse(args).InvokeAsync(new InvocationConfiguration { Output = output, Error = error });
        _output = output.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
        _error = error.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static string Line(TrackedLocation l) =>
        $"{l.Id} {GeocodingClient.Format(l.Name, l.Latitude, l.Longitude)}";

    private static string Result(string name, double latitude, double longitude) =>
        string.Create(CultureInfo.InvariantCulture, $$"""{"id":1,"name":"{{name}}","latitude":{{latitude}},"longitude":{{longitude}},"country":"X"}""");

    private static string ResultsBody(params string[] results) =>
        $$"""{"results":[{{string.Join(',', results)}}],"generationtime_ms":0.2}""";

    private sealed class StubHandler : HttpMessageHandler
    {
        public string? Body { get; set; }

        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add($"{request.Method} {request.RequestUri}");
            if (Body is null)
            {
                throw new HttpRequestException("no answer");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Body, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}
