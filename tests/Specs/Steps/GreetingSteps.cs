using System.CommandLine;
using Core;
using Reqnroll;
using Xunit;

namespace Specs.Steps;

[Binding]
public sealed class GreetingSteps
{
    private string _name = "";
    private string _greeting = "";
    private string _output = "";
    private int _exitCode;

    [Given("the name {string}")]
    public void GivenTheName(string name) => _name = name;

    [When("the greeter greets")]
    public void WhenTheGreeterGreets() => _greeting = Greeter.Greet(_name);

    [Then("the greeting is {string}")]
    public void ThenTheGreetingIs(string expected) => Assert.Equal(expected, _greeting);

    [When("the console program runs with the arguments {string}")]
    public void WhenTheConsoleProgramRuns(string arguments)
    {
        string[] args = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        using var output = new StringWriter();
        _exitCode = GreetCommand.Create().Parse(args).Invoke(new InvocationConfiguration { Output = output });
        _output = output.ToString();
    }

    [Then("the console program prints {string}")]
    public void ThenTheConsoleProgramPrints(string expected)
    {
        Assert.Equal(0, _exitCode);
        Assert.Equal(expected, _output.TrimEnd('\r', '\n'));
    }

    [Then("the console program prints a line that contains {string}")]
    public void ThenTheConsoleProgramPrintsALineThatContains(string expected)
    {
        Assert.Equal(0, _exitCode);
        Assert.Contains(_output.Split('\n'), line => line.Contains(expected, StringComparison.Ordinal));
    }
}
