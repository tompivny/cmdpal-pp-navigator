using FluentAssertions;
using PowerPlatformNavigator.Services;
using System;
using Xunit;

namespace PowerPlatformNavigator.Tests;

public sealed class CloudEndpointsTests
{
    [Fact]
    public void GetDiscoveryBaseUrl_ReturnsAbsoluteHttpsUrl_ForAllClouds()
    {
        foreach (PowerPlatformCloud cloud in Enum.GetValues<PowerPlatformCloud>())
        {
            string url = PowerPlatformCloudEndpoints.GetDiscoveryBaseUrl(cloud);

            url.Should().NotBeNullOrWhiteSpace();
            Uri.TryCreate(url, UriKind.Absolute, out Uri? uri).Should().BeTrue();
            uri!.Scheme.Should().Be(Uri.UriSchemeHttps);
        }
    }
}
