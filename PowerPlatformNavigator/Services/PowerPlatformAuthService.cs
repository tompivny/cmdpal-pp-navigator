using Microsoft.Identity.Client;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace PowerPlatformNavigator.Services;

internal sealed class PowerPlatformAuthService
{
    internal const string SampleClientId = "51f81489-12ee-4a9e-aaae-a2591f45987d";

    private const string RedirectUri = "http://localhost";
    private const string Authority = "https://login.microsoftonline.com/organizations/";

    private static readonly string CacheFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PowerPlatformNavigator",
        "msal.cache");

    private static readonly object CacheFileLock = new();

    private readonly IPublicClientApplication _publicClientApplication;

    public PowerPlatformAuthService()
    {
        _publicClientApplication = PublicClientApplicationBuilder
            .Create(SampleClientId)
            .WithAuthority(Authority)
            .WithRedirectUri(RedirectUri)
            .Build();

        RegisterTokenCache(_publicClientApplication.UserTokenCache);
    }

    public async Task<string> GetDiscoveryAccessTokenAsync(string discoveryBaseUrl, CancellationToken cancellationToken = default)
    {
        var trimmedBaseUrl = discoveryBaseUrl.TrimEnd('/');
        string[] scopes = [$"{trimmedBaseUrl}/user_impersonation"];

        var account = (await _publicClientApplication.GetAccountsAsync().ConfigureAwait(false)).FirstOrDefault();

        try
        {
            var silentResult = await _publicClientApplication
                .AcquireTokenSilent(scopes, account)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return silentResult.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            var interactiveResult = await _publicClientApplication
                .AcquireTokenInteractive(scopes)
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return interactiveResult.AccessToken;
        }
    }

    private static void RegisterTokenCache(ITokenCache tokenCache)
    {
        tokenCache.SetBeforeAccess(args =>
        {
            lock (CacheFileLock)
            {
                try
                {
                    if (!File.Exists(CacheFilePath))
                    {
                        return;
                    }

                    byte[] protectedBytes = File.ReadAllBytes(CacheFilePath);
                    byte[] unprotectedBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                    args.TokenCache.DeserializeMsalV3(unprotectedBytes);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine($"Token cache read failed: {exception.Message}");
                }
            }
        });

        tokenCache.SetAfterAccess(args =>
        {
            if (!args.HasStateChanged)
            {
                return;
            }

            lock (CacheFileLock)
            {
                try
                {
                    string? cacheDirectory = Path.GetDirectoryName(CacheFilePath);
                    if (!string.IsNullOrWhiteSpace(cacheDirectory))
                    {
                        Directory.CreateDirectory(cacheDirectory);
                    }

                    byte[] rawBytes = args.TokenCache.SerializeMsalV3();
                    byte[] protectedBytes = ProtectedData.Protect(rawBytes, null, DataProtectionScope.CurrentUser);
                    File.WriteAllBytes(CacheFilePath, protectedBytes);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine($"Token cache write failed: {exception.Message}");
                }
            }
        });
    }
}