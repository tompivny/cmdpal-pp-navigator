using PowerPlatformNavigator.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PowerPlatformNavigator.Services;

internal sealed class PowerPlatformDiscoveryService
{
    private static readonly HttpClient HttpClient = new();

    private readonly PowerPlatformAuthService _authService;

    public PowerPlatformDiscoveryService(PowerPlatformAuthService authService)
    {
        _authService = authService;
    }

    public async Task<IReadOnlyList<PowerPlatformEnvironment>> GetEnvironmentsAsync(
        PowerPlatformCloud cloud = PowerPlatformCloud.Commercial,
        CancellationToken cancellationToken = default)
    {
        string discoveryBaseUrl = PowerPlatformCloudEndpoints.GetDiscoveryBaseUrl(cloud);
        string accessToken = await _authService.GetDiscoveryAccessTokenAsync(discoveryBaseUrl, cancellationToken).ConfigureAwait(false);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{discoveryBaseUrl}/api/discovery/v2.0/Instances"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var environments = ParseEnvironments(json);
        return environments
            .OrderBy(environment => environment.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static List<PowerPlatformEnvironment> ParseEnvironments(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("value", out var valuesElement) || valuesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var environments = new List<PowerPlatformEnvironment>();
        foreach (JsonElement item in valuesElement.EnumerateArray())
        {
            string? friendlyName = ReadString(item, "FriendlyName", "friendlyName", "DisplayName", "displayName");
            string? environmentId = ReadString(item, "EnvironmentId", "environmentId");
            string? organizationUrl = ReadString(item, "Url", "url", "OrganizationUrl", "organizationUrl");
            string? uniqueName = ReadString(item, "UniqueName", "uniqueName", "UrlName", "urlName");
            string? region = ReadString(item, "Geo", "geo", "Region", "region");
            string? environmentType = ReadString(item, "EnvironmentType", "environmentType", "Type", "type");

            if (string.IsNullOrWhiteSpace(friendlyName) && string.IsNullOrWhiteSpace(uniqueName))
            {
                Debug.WriteLine("Skipping environment record due to missing display metadata.");
                continue;
            }

            environments.Add(new PowerPlatformEnvironment
            {
                FriendlyName = friendlyName ?? uniqueName!,
                EnvironmentId = environmentId,
                OrganizationUrl = organizationUrl,
                UniqueName = uniqueName,
                Region = region,
                EnvironmentType = environmentType,
            });
        }

        return environments;
    }

    internal static string? ReadString(JsonElement jsonElement, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (!jsonElement.TryGetProperty(propertyName, out var valueElement))
            {
                continue;
            }

            if (valueElement.ValueKind == JsonValueKind.String)
            {
                return valueElement.GetString();
            }
        }

        return null;
    }
}