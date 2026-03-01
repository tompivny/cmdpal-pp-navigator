namespace PowerPlatformNavigator.Services;

internal enum PowerPlatformCloud
{
    Commercial,
    Gcc,
    Usg,
    Dod,
    China,
}

internal static class PowerPlatformCloudEndpoints
{
    public static string GetDiscoveryBaseUrl(PowerPlatformCloud cloud) =>
        cloud switch
        {
            PowerPlatformCloud.Commercial => "https://globaldisco.crm.dynamics.com",
            PowerPlatformCloud.Gcc => "https://globaldisco.crm9.dynamics.com",
            PowerPlatformCloud.Usg => "https://globaldisco.crm.microsoftdynamics.us",
            PowerPlatformCloud.Dod => "https://globaldisco.crm.appsplatform.us",
            PowerPlatformCloud.China => "https://globaldisco.crm.dynamics.cn",
            _ => "https://globaldisco.crm.dynamics.com",
        };
}