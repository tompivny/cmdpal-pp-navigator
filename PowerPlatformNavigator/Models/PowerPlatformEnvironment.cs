namespace PowerPlatformNavigator.Models;

internal sealed class PowerPlatformEnvironment
{
    public required string FriendlyName { get; init; }

    public string? EnvironmentId { get; init; }

    public string? OrganizationUrl { get; init; }

    public string? UniqueName { get; init; }

    public string? Region { get; init; }

    public string? EnvironmentType { get; init; }
}