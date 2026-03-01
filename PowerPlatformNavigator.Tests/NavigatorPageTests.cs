using FluentAssertions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using PowerPlatformNavigator.Models;
using Xunit;

namespace PowerPlatformNavigator.Tests;

public sealed class NavigatorPageTests
{
    [Fact]
    public void BuildSubtitle_IncludesHostRegionTypeAndUniqueName_WhenAvailable()
    {
        var environment = new PowerPlatformEnvironment
        {
            FriendlyName = "Contoso",
            OrganizationUrl = "https://contoso.crm.dynamics.com",
            Region = "United States",
            EnvironmentType = "Sandbox",
            UniqueName = "contoso-dev",
        };

        string subtitle = PowerPlatformNavigatorPage.BuildSubtitle(environment);

        subtitle.Should().Be("contoso.crm.dynamics.com • United States • Sandbox • contoso-dev");
    }

    [Fact]
    public void BuildSubtitle_OmitsMissingValues()
    {
        var environment = new PowerPlatformEnvironment
        {
            FriendlyName = "Contoso",
            Region = "Europe",
        };

        string subtitle = PowerPlatformNavigatorPage.BuildSubtitle(environment);

        subtitle.Should().Be("Europe");
    }

    [Fact]
    public void BuildPrimaryCommand_ReturnsOpenUrlCommand_WhenAtLeastOneUrlExists()
    {
        object command = PowerPlatformNavigatorPage.BuildPrimaryCommand(null, "https://org.crm.dynamics.com", null);

        command.Should().BeOfType<OpenUrlCommand>();
    }

    [Fact]
    public void BuildPrimaryCommand_ReturnsNoOpCommand_WhenNoUrlExists()
    {
        object command = PowerPlatformNavigatorPage.BuildPrimaryCommand(null, null, null);

        command.Should().BeOfType<NoOpCommand>();
    }
}
