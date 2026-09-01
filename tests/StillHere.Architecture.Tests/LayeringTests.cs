using Shouldly;
using Xunit;

namespace StillHere.Architecture.Tests;

public sealed class LayeringTests
{
    [Fact]
    public void SourceFiles_OutsideInfrastructure_DoNotReferenceEntities()
    {
        var root = FindRepositoryRoot();
        var offenders = Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("StillHere.Infrastructure", StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path).Contains("StillHere.Infrastructure.Persistence.Entities", StringComparison.Ordinal))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Application_HasNoProjectReferences()
    {
        var root = FindRepositoryRoot();
        var csproj = Path.Combine(root, "src", "StillHere.Application", "StillHere.Application.csproj");

        File.ReadAllText(csproj).ShouldNotContain("ProjectReference");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "StillHere.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("StillHere.slnx was not found.");
    }
}
