using FluentAssertions;
using PowerPlatformNavigator.Models;
using PowerPlatformNavigator.Services;
using Xunit;

namespace PowerPlatformNavigator.Tests;

public sealed class UrlBuilderTests
{
    [Fact]
    public void TryBuildMakerPortalUrl_ReturnsExpectedUrl_WhenEnvironmentIdExists()
    {
        var environment = new PowerPlatformEnvironment
        {
            FriendlyName = "Test",
            EnvironmentId = "env-123",
        };

        string? result = PowerPlatformUrlBuilder.TryBuildMakerPortalUrl(environment);

        result.Should().Be("https://make.powerapps.com/environments/env-123/home");
    }

    [Fact]
    public void TryBuildMakerPortalUrl_ReturnsNull_WhenEnvironmentIdMissing()
    {
        var environment = new PowerPlatformEnvironment
        {
            FriendlyName = "Test",
            EnvironmentId = null,
        };

        string? result = PowerPlatformUrlBuilder.TryBuildMakerPortalUrl(environment);

        result.Should().BeNull();
    }

    [Fact]
    public void TryBuildPpacUrl_ReturnsExpectedUrl_WhenEnvironmentIdExists()
    {
        var environment = new PowerPlatformEnvironment
        {
            FriendlyName = "Test",
            EnvironmentId = "env-123",
        };

        string? result = PowerPlatformUrlBuilder.TryBuildPpacUrl(environment);

        result.Should().Be("https://admin.powerplatform.microsoft.com/environments/env-123/hub");
    }

    [Fact]
    public void TryBuildPpacUrl_ReturnsNull_WhenEnvironmentIdMissing()
    {
        var environment = new PowerPlatformEnvironment
        {
            FriendlyName = "Test",
        };

        string? result = PowerPlatformUrlBuilder.TryBuildPpacUrl(environment);

        result.Should().BeNull();
    }

    [Fact]
    public void TryBuildDynamicsUrl_ReturnsAbsoluteUrl_WhenOrganizationUrlIsValid()
    {
        var environment = new PowerPlatformEnvironment
        {
            FriendlyName = "Test",
            OrganizationUrl = "https://org.crm.dynamics.com/",
        };

        string? result = PowerPlatformUrlBuilder.TryBuildDynamicsUrl(environment);

        result.Should().Be("https://org.crm.dynamics.com/");
    }

    [Fact]
    public void TryBuildDynamicsUrl_ReturnsNull_WhenOrganizationUrlIsNotAbsolute()
    {
        var environment = new PowerPlatformEnvironment
        {
            FriendlyName = "Test",
            OrganizationUrl = "/relative/url",
        };

        string? result = PowerPlatformUrlBuilder.TryBuildDynamicsUrl(environment);

        result.Should().BeNull();
    }
}
