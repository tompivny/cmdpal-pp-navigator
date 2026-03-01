// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Identity.Client;
using PowerPlatformNavigator.Models;
using PowerPlatformNavigator.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PowerPlatformNavigator;

internal sealed partial class PowerPlatformNavigatorPage : ListPage
{
    private readonly object _itemsLock = new();
    private readonly PowerPlatformDiscoveryService _discoveryService;

    private List<IListItem> _items = [];
    private bool _loadStarted;

    public PowerPlatformNavigatorPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\PowerPlatformLogo.png");
        Title = "Power Platform Navigator";
        Name = "Open";
        PlaceholderText = "Search environments";
        _discoveryService = new PowerPlatformDiscoveryService(new PowerPlatformAuthService());
    }

    public override IListItem[] GetItems()
    {
        EnsureItemsLoaded();

        lock (_itemsLock)
        {
            return [.. _items];
        }
    }

    private void EnsureItemsLoaded()
    {
        if (_loadStarted)
        {
            return;
        }

        _loadStarted = true;
        _ = Task.Run(LoadEnvironmentsAsync);
    }

    private async Task LoadEnvironmentsAsync()
    {
        IsLoading = true;
        RaiseItemsChanged();

        try
        {
            var environments = await _discoveryService
                .GetEnvironmentsAsync(PowerPlatformCloud.Commercial)
                .ConfigureAwait(false);

            List<IListItem> newItems = BuildEnvironmentItems(environments);

            if (newItems.Count == 0)
            {
                newItems.Add(new ListItem(new NoOpCommand())
                {
                    Title = "No environments found",
                    Subtitle = "Sign in completed, but discovery returned no environments.",
                });
            }

            lock (_itemsLock)
            {
                _items = newItems;
            }
        }
        catch (MsalException exception)
        {
            Debug.WriteLine($"Authentication failed: {exception.Message}");
            SetSingleStateItem("Sign-in required", "Authentication was canceled or failed. Open this page again to retry.");
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Environment discovery failed: {exception.Message}");
            SetSingleStateItem("Unable to load environments", "Check connectivity and try again.");
        }
        finally
        {
            IsLoading = false;
            RaiseItemsChanged();
        }
    }

    private void SetSingleStateItem(string title, string subtitle)
    {
        lock (_itemsLock)
        {
            _items = [new ListItem(new NoOpCommand()) { Title = title, Subtitle = subtitle }];
        }
    }

    private static List<IListItem> BuildEnvironmentItems(IReadOnlyList<PowerPlatformEnvironment> environments)
    {
        return environments.Select(BuildEnvironmentItem).Cast<IListItem>().ToList();
    }

    private static ListItem BuildEnvironmentItem(PowerPlatformEnvironment environment)
    {
        string? makerUrl = PowerPlatformUrlBuilder.TryBuildMakerPortalUrl(environment);
        string? dynamicsUrl = PowerPlatformUrlBuilder.TryBuildDynamicsUrl(environment);
        string? ppacUrl = PowerPlatformUrlBuilder.TryBuildPpacUrl(environment);

        ICommand primaryCommand = BuildPrimaryCommand(makerUrl, dynamicsUrl, ppacUrl);

        var item = new ListItem(primaryCommand)
        {
            Title = environment.FriendlyName,
            Subtitle = BuildSubtitle(environment),
            TextToSuggest = string.Join(' ', new[] { environment.FriendlyName, environment.UniqueName, environment.Region, environment.EnvironmentType }.Where(value => !string.IsNullOrWhiteSpace(value))),
        };

        item.MoreCommands =
        [
            BuildContextCommand("Open in Dynamics", dynamicsUrl, "Dynamics URL unavailable"),
            BuildContextCommand("Open in PPAC", ppacUrl, "PPAC URL unavailable"),
        ];

        return item;
    }

    internal static ICommand BuildPrimaryCommand(string? makerUrl, string? dynamicsUrl, string? ppacUrl)
    {
        if (!string.IsNullOrWhiteSpace(makerUrl))
        {
            return new OpenUrlCommand(makerUrl);
        }

        if (!string.IsNullOrWhiteSpace(dynamicsUrl))
        {
            return new OpenUrlCommand(dynamicsUrl);
        }

        if (!string.IsNullOrWhiteSpace(ppacUrl))
        {
            return new OpenUrlCommand(ppacUrl);
        }

        return new NoOpCommand();
    }

    private static CommandContextItem BuildContextCommand(string title, string? url, string unavailableTitle)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            return new CommandContextItem(new OpenUrlCommand(url))
            {
                Title = title,
            };
        }

        return new CommandContextItem(new NoOpCommand())
        {
            Title = unavailableTitle,
        };
    }

    internal static string BuildSubtitle(PowerPlatformEnvironment environment)
    {
        string? organizationHost = null;
        if (Uri.TryCreate(environment.OrganizationUrl, UriKind.Absolute, out var organizationUri))
        {
            organizationHost = organizationUri.Host;
        }

        string[] parts =
        [
            organizationHost ?? string.Empty,
            environment.Region ?? string.Empty,
            environment.EnvironmentType ?? string.Empty,
            environment.UniqueName ?? string.Empty,
        ];

        return string.Join(" • ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}