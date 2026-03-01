using PowerPlatformNavigator.Models;
using System;

namespace PowerPlatformNavigator.Services;

internal static class PowerPlatformUrlBuilder
{
    public static string? TryBuildMakerPortalUrl(PowerPlatformEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(environment.EnvironmentId))
        {
            return null;
        }

        string escapedEnvironmentId = Uri.EscapeDataString(environment.EnvironmentId);
        return $"https://make.powerapps.com/environments/{escapedEnvironmentId}/home";
    }

    public static string? TryBuildPpacUrl(PowerPlatformEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(environment.EnvironmentId))
        {
            return null;
        }

        string escapedEnvironmentId = Uri.EscapeDataString(environment.EnvironmentId);
        return $"https://admin.powerplatform.microsoft.com/environments/{escapedEnvironmentId}/hub";
    }

    public static string? TryBuildDynamicsUrl(PowerPlatformEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(environment.OrganizationUrl))
        {
            return null;
        }

        return Uri.TryCreate(environment.OrganizationUrl, UriKind.Absolute, out var uri) ? uri.ToString() : null;
    }
}