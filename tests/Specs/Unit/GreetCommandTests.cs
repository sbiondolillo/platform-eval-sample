using System.CommandLine;
using Core;
using Xunit;

namespace Specs.Unit;

public sealed class GreetCommandTests
{
    private static string Run(params string[] args)
    {
        using var output = new StringWriter();
        GreetCommand.Create().Parse(args).Invoke(new InvocationConfiguration { Output = output });
        return output.ToString().TrimEnd('\r', '\n');
    }

    [Fact]
    public void GreetsWithoutShout() => Assert.Equal("Hello, Ada!", Run("Ada"));

    [Fact]
    public void ShoutsAName() => Assert.Equal("HELLO, ADA!", Run("Ada", "--shout"));

    [Fact]
    public void ShoutsTheWorld() => Assert.Equal("HELLO, WORLD!", Run("--shout"));

    [Fact]
    public void TreeHoldsTheOptionShout() =>
        Assert.Contains(GreetCommand.Create().Options, option => option.Name is "--shout");
}
