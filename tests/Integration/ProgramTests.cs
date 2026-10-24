using System.Diagnostics;
using Xunit;

namespace Integration;

public sealed class ProgramTests
{
    private static readonly string[] RunPrefix = ["run", "--no-restore", "--project", "src/App", "--"];

    private static string RepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Solution.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Solution.slnx not found.");
    }

    private static async Task<(int ExitCode, string Out, string Err)> RunAsync(params string[] args)
    {
        ProcessStartInfo info = new("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = RepoRoot(),
        };
        foreach (string a in RunPrefix.Concat(args))
        {
            info.ArgumentList.Add(a);
        }

        using Process process = Process.Start(info)!;
        CancellationToken token = TestContext.Current.CancellationToken;
        Task<string> output = process.StandardOutput.ReadToEndAsync(token);
        Task<string> error = process.StandardError.ReadToEndAsync(token);
        await process.WaitForExitAsync(token);
        return (process.ExitCode, await output, await error);
    }

    [Fact]
    public async Task AddPrintsOnlyTheAddedLine()
    {
        string database = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
        try
        {
            (int exitCode, string output, _) = await RunAsync("add", "Kathmandu", "--database", database);
            Assert.Equal(0, exitCode);
            Assert.Equal("Added Kathmandu (27.70169, 85.3206)", output.TrimEnd());
        }
        finally
        {
            File.Delete(database);
        }
    }

    [Fact]
    public async Task AFailedAddLeavesStandardOutputEmpty()
    {
        string database = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
        try
        {
            (int exitCode, string output, string error) = await RunAsync("add", "Zzzzqqqxxx", "--database", database);
            Assert.Equal(1, exitCode);
            Assert.Equal("", output);
            Assert.Contains("Zzzzqqqxxx", error, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(database);
        }
    }
}
