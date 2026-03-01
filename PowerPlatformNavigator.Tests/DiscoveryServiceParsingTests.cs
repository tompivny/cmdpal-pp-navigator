using FluentAssertions;
using PowerPlatformNavigator.Services;
using Xunit;

namespace PowerPlatformNavigator.Tests;

public sealed class DiscoveryServiceParsingTests
{
    [Fact]
    public void ParseEnvironments_ParsesStandardCasing()
    {
        const string json = """
        {
          "value": [
            {
              "FriendlyName": "Contoso Dev",
              "EnvironmentId": "env-001",
              "Url": "https://contoso.crm.dynamics.com",
              "UniqueName": "contoso-dev",
              "Geo": "United States",
              "EnvironmentType": "Sandbox"
            }
          ]
        }
        """;

        var result = PowerPlatformDiscoveryService.ParseEnvironments(json);

        result.Should().HaveCount(1);
        result[0].FriendlyName.Should().Be("Contoso Dev");
        result[0].EnvironmentId.Should().Be("env-001");
        result[0].OrganizationUrl.Should().Be("https://contoso.crm.dynamics.com");
        result[0].UniqueName.Should().Be("contoso-dev");
        result[0].Region.Should().Be("United States");
        result[0].EnvironmentType.Should().Be("Sandbox");
    }

    [Fact]
    public void ParseEnvironments_ParsesAlternateCasingAndFallbackDisplayName()
    {
        const string json = """
        {
          "value": [
            {
              "displayName": "Tailspin Prod",
              "environmentId": "env-002",
              "organizationUrl": "https://tailspin.crm.dynamics.com",
              "urlName": "tailspin-prod",
              "region": "Europe",
              "type": "Production"
            }
          ]
        }
        """;

        var result = PowerPlatformDiscoveryService.ParseEnvironments(json);

        result.Should().HaveCount(1);
        result[0].FriendlyName.Should().Be("Tailspin Prod");
        result[0].EnvironmentId.Should().Be("env-002");
        result[0].UniqueName.Should().Be("tailspin-prod");
        result[0].Region.Should().Be("Europe");
        result[0].EnvironmentType.Should().Be("Production");
    }

    [Fact]
    public void ParseEnvironments_UsesUniqueName_WhenFriendlyNameMissing()
    {
        const string json = """
        {
          "value": [
            {
              "environmentId": "env-003",
              "uniqueName": "fabrikam-dev"
            }
          ]
        }
        """;

        var result = PowerPlatformDiscoveryService.ParseEnvironments(json);

        result.Should().HaveCount(1);
        result[0].FriendlyName.Should().Be("fabrikam-dev");
    }

    [Fact]
    public void ParseEnvironments_SkipsItem_WhenBothFriendlyNameAndUniqueNameMissing()
    {
        const string json = """
        {
          "value": [
            {
              "environmentId": "env-004"
            }
          ]
        }
        """;

        var result = PowerPlatformDiscoveryService.ParseEnvironments(json);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseEnvironments_ReturnsEmpty_WhenValuePropertyMissingOrNotArray()
    {
        const string missingValueJson = """
        {
          "items": []
        }
        """;

        const string nonArrayValueJson = """
        {
          "value": {}
        }
        """;

        PowerPlatformDiscoveryService.ParseEnvironments(missingValueJson).Should().BeEmpty();
        PowerPlatformDiscoveryService.ParseEnvironments(nonArrayValueJson).Should().BeEmpty();
    }
}
