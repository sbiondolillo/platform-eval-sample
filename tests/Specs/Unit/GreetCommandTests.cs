using System.CommandLine;
using Core;
using Xunit;

namespace Specs.Unit;

public sealed class GreetCommandTests
{
    private static string Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        GreetCommand.Create().Parse(args).Invoke(new InvocationConfiguration { Output = output, Error = error });
        return output.ToString().TrimEnd('\r', '\n');
    }

    [Fact]
    public void GreetsInMixedCaseWithoutTheOption() => Assert.Equal("Hello, Ada!", Run("Ada"));

    [Fact]
    public void ShoutsAName() => Assert.Equal("HELLO, ADA!", Run("Ada", "--shout"));

    [Fact]
    public void ShoutsTheDefaultName() => Assert.Equal("HELLO, WORLD!", Run("--shout"));

    [Fact]
    public void ListsShoutInTheHelp() => Assert.Contains("--shout", Run("--help"), StringComparison.Ordinal);
}
