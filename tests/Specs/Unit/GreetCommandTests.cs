using System.CommandLine;
using Core;
using Xunit;

namespace Specs.Unit;

public sealed class GreetCommandTests
{
    private static (int ExitCode, string Output) Run(params string[] args)
    {
        using var output = new StringWriter();
        int exitCode = GreetCommand.Create().Parse(args).Invoke(new InvocationConfiguration { Output = output, Error = output });
        return (exitCode, output.ToString().TrimEnd('\r', '\n'));
    }

    [Fact]
    public void WithoutShoutTheGreetingKeepsItsCase() => Assert.Equal((0, "Hello, Ada!"), Run("Ada"));

    [Fact]
    public void ShoutUpperCasesTheGreeting() => Assert.Equal((0, "HELLO, ADA!"), Run("Ada", "--shout"));

    [Fact]
    public void ShoutUpperCasesTheDefaultName() => Assert.Equal((0, "HELLO, WORLD!"), Run("--shout"));

    [Fact]
    public void TheTreeHoldsTheOptionShout() =>
        Assert.Contains(GreetCommand.Create().Options, option => option.Name is "--shout");
}
