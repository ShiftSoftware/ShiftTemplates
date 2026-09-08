using Microsoft.Extensions.Configuration;
using ShiftSoftware.ShiftFrameworkTestingTools;
using StockPlusPlus.API;
using StockPlusPlus.Data.DbContext;

namespace StockPlusPlus.Test.Tests;

public class TestFactoryConfigurationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefaultFactory_IgnoresEnvironmentConnectionStrings(bool useTestPrefix)
    {
        var name = $"ConfigurationProbe_{Guid.NewGuid():N}";
        var variable = $"{(useTestPrefix ? "SHIFT_TEST_" : "")}ConnectionStrings__{name}";
        var previous = Environment.GetEnvironmentVariable(variable);

        try
        {
            Environment.SetEnvironmentVariable(variable, "unintended-database");
            using var factory = new DefaultConfigurationProbe();
            var configuration = factory.ReadConfiguration();
            using var configurationLifetime = configuration as IDisposable;

            Assert.Null(configuration.GetConnectionString(name));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, previous);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SampleFactory_OnlyAcceptsExplicitlyPrefixedConnectionStrings(bool useTestPrefix)
    {
        var name = $"ConfigurationProbe_{Guid.NewGuid():N}";
        var variable = $"{(useTestPrefix ? "SHIFT_TEST_" : "")}ConnectionStrings__{name}";
        var previous = Environment.GetEnvironmentVariable(variable);

        try
        {
            Environment.SetEnvironmentVariable(variable, "isolated-test-database");
            using var factory = new SampleConfigurationProbe();
            var configuration = factory.ReadConfiguration();
            using var configurationLifetime = configuration as IDisposable;

            Assert.Equal(useTestPrefix ? "isolated-test-database" : null, configuration.GetConnectionString(name));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, previous);
        }
    }

    // Reading configuration never starts either host or opens a database. Unique keys keep these tests isolated
    // from other tests and from any connection strings supplied to the real SQL verification run.
    private sealed class DefaultConfigurationProbe() : ShiftCustomWebApplicationFactory<WebMarker, DB>(
        "SQLServer_Test", new ShiftCustomWebApplicationBearerAuthSettings())
    {
        public IConfigurationRoot ReadConfiguration() => CreateTestConfigurationBuilder().Build();
    }

    private sealed class SampleConfigurationProbe : CustomWebApplicationFactory
    {
        public IConfigurationRoot ReadConfiguration() => CreateTestConfigurationBuilder().Build();
    }
}
