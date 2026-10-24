using System.CommandLine;
using Core;
using Xunit;

namespace Specs.Unit;

public sealed class GreetCommandTests
{
    private static (int ExitCode, string Output) Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        int exitCode = GreetCommand.Create().Parse(args).Invoke(new InvocationConfiguration { Output = output, Error = error });
        return (exitCode, output.ToString().TrimEnd('\r', '\n'));
    }

    [Fact]
    public void WithoutShoutTheGreetingKeepsItsCase() =>
        Assert.Equal((0, "Hello, Ada!"), Run("Ada"));

    [Fact]
    public void ShoutPrintsUpperCase() =>
        Assert.Equal((0, "HELLO, ADA!"), Run("Ada", "--shout"));

    [Fact]
    public void ShoutAloneGreetsTheWorld() =>
        Assert.Equal((0, "HELLO, WORLD!"), Run("--shout"));
}
