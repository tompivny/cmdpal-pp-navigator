// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace PowerPlatformNavigator;

public partial class PowerPlatformNavigatorCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public PowerPlatformNavigatorCommandsProvider()
    {
        DisplayName = "Power Platform Navigator";
        Icon = IconHelpers.FromRelativePath("Assets\\PowerPlatformLogo.png");
        _commands = [
            new CommandItem(new PowerPlatformNavigatorPage()) { Title = DisplayName },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
