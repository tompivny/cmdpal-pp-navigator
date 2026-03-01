# Power Platform Navigator (PowerToys Command Palette Extension)

Power Platform Navigator is a [PowerToys Command Palette](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/overview) extension that helps you quickly open Power Platform environments in the right destination.

It discovers environments from the [Global Discovery Service](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/discovery-service), shows them in a searchable list, and opens the selected environment in your browser.

## What it does

- Adds a top-level **Power Platform Navigator** command in Command Palette.
- Authenticates with Microsoft Entra ID using interactive sign-in.
- Discovers environments from the [Global Discovery Service](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/discovery-service).
- Shows each environment with clear metadata (host, region, type, unique name).
- Opens canonical URLs for:
  - Maker portal (primary action)
  - Dynamics organization URL
  - Power Platform Admin Center (PPAC)

## Tech stack

- .NET 9 (`net9.0-windows10.0.26100.0`)
- C# + WinRT COM extension host
- `Microsoft.CommandPalette.Extensions`
- `Microsoft.Identity.Client` (MSAL)
- xUnit + FluentAssertions for unit tests

## Prerequisites

- Windows with PowerToys Command Palette installed
- .NET 9 SDK
- Windows SDK `10.0.26100` (or compatible with project target)
- PowerShell 7+ (`pwsh`)

One-time SDK install (if needed):

```powershell
winget install --id Microsoft.WindowsSDK.10.0.26100
```

## Quick start

1. Restore/build dependencies:

   ```powershell
   dotnet restore
   ```

2. Run tests:

   ```powershell
   dotnet test ./PowerPlatformNavigator.Tests/PowerPlatformNavigator.Tests.csproj --configuration Debug
   ```

3. Build, package, install, and refresh Command Palette:

   ```powershell
   pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/Deploy-CommandPaletteExtension.ps1 -Platform x64 -Configuration Debug -InstallMissingSdk
   ```

### VS Code tasks

- **Run Tests**
- **CmdPal: Deploy extension (x64)**

These tasks are already configured in the workspace.

## Using the extension

1. Open PowerToys Command Palette.
2. Launch **Power Platform Navigator**.
3. Sign in when prompted.
4. Search and select an environment.
5. Press Enter to open the primary destination (Maker portal), or use context actions for Dynamics/PPAC.

## Authentication and discovery details

- Authority: `https://login.microsoftonline.com/organizations/`
- Public client ID: `51f81489-12ee-4a9e-aaae-a2591f45987d`
- Commercial discovery endpoint:
  - `https://globaldisco.crm.dynamics.com/api/discovery/v2.0/Instances`
- Token cache path:
  - `%LOCALAPPDATA%\PowerPlatformNavigator\msal.cache`

Cloud base URLs implemented for discovery mapping:

- Commercial: `https://globaldisco.crm.dynamics.com`
- GCC: `https://globaldisco.crm9.dynamics.com`
- USG: `https://globaldisco.crm.microsoftdynamics.us`
- DoD: `https://globaldisco.crm.appsplatform.us`
- China: `https://globaldisco.crm.dynamics.cn`

## URL generation

Navigation URLs are centralized in `PowerPlatformNavigator/Services/PowerPlatformUrlBuilder.cs`:

- Maker: `https://make.powerapps.com/environments/{environmentId}/home`
- PPAC: `https://admin.powerplatform.microsoft.com/environments/{environmentId}/hub`
- Dynamics: validated absolute `OrganizationUrl`

## Testing

Run all unit tests:

```powershell
dotnet test ./PowerPlatformNavigator.Tests/PowerPlatformNavigator.Tests.csproj --configuration Debug
```

Current test coverage includes:

- Cloud endpoint mappings
- Discovery JSON parsing behavior and fallback handling
- URL builder outputs and null-path behavior
- Navigator page command/subtitle behavior

## Local deployment behavior

`scripts/Deploy-CommandPaletteExtension.ps1` handles the local deploy loop:

- Validates required Windows SDK
- Creates/reuses local dev signing certificate
- Builds signed MSIX package
- Installs package with `Add-AppxPackage`
- Restarts Command Palette when possible

The script defaults to a local certificate password of `cmdpaldev`.
Override with `CMDPAL_DEV_CERT_PASSWORD` if desired.
