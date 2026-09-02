using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StillHere.Application;
using Xunit;

namespace StillHere.Infrastructure.Tests;

public sealed class SqliteDataSourceDirectoryTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"stillhere-test-{Guid.NewGuid():N}");

    [Fact]
    public void AddInfrastructure_DataSourceInNonExistentDirectory_CreatesDirectory()
    {
        var dbPath = Path.Combine(_tempRoot, "nested", "stillhere.db");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={dbPath}",
            })
            .Build();

        Directory.Exists(Path.GetDirectoryName(dbPath)).ShouldBeFalse();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        services.AddApplication();

        Directory.Exists(Path.GetDirectoryName(dbPath)).ShouldBeTrue();
    }

    [Fact]
    public void AddInfrastructure_InMemoryDataSource_DoesNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Data Source=:memory:",
            })
            .Build();

        var services = new ServiceCollection();

        Should.NotThrow(() =>
        {
            services.AddInfrastructure(configuration);
            services.AddApplication();
        });
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
