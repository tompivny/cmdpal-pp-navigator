[CmdletBinding()]
param(
    [ValidateSet('x64', 'ARM64')]
    [string]$Platform = 'x64',
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$InstallMissingSdk,
    [switch]$SkipCommandPaletteRestart
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectFile = Join-Path $repoRoot 'PowerPlatformNavigator\PowerPlatformNavigator.csproj'
$certScript = Join-Path $repoRoot 'scripts\New-DevCertificate.ps1'
$appxOutputDir = Join-Path $repoRoot 'artifacts\AppPackages\'
$certificatePasswordPlain = if ($env:CMDPAL_DEV_CERT_PASSWORD) { $env:CMDPAL_DEV_CERT_PASSWORD } else { 'cmdpaldev' }

function Get-ProjectTargetPlatformVersion {
    param([string]$CsprojPath)

    [xml]$projectXml = Get-Content -LiteralPath $CsprojPath
    $targetFrameworkNode = $projectXml.Project.PropertyGroup.TargetFramework | Select-Object -First 1

    if (-not $targetFrameworkNode) {
        throw 'TargetFramework was not found in the project file.'
    }

    $targetFramework = [string]$targetFrameworkNode
    if ($targetFramework -match 'windows(?<version>\d+\.\d+\.\d+\.\d+)') {
        return $Matches.version
    }

    throw "Unable to parse Windows target platform version from TargetFramework '$targetFramework'."
}

function Test-UapSdkInstalled {
    param([string]$SdkVersion)

    $candidateVersionPaths = @(
        "C:\Program Files (x86)\Windows Kits\10\Platforms\UAP\$SdkVersion",
        "C:\Program Files\Windows Kits\10\Platforms\UAP\$SdkVersion",
        "C:\Program Files (x86)\Windows Kits\10\DesignTime\CommonConfiguration\Neutral\UAP\$SdkVersion",
        "C:\Program Files\Windows Kits\10\DesignTime\CommonConfiguration\Neutral\UAP\$SdkVersion"
    )

    $requiredMarkers = @('Platform.xml', 'UAP.props', 'Features.xml')

    foreach ($versionPath in $candidateVersionPaths) {
        if (-not (Test-Path -LiteralPath $versionPath)) {
            continue
        }

        foreach ($marker in $requiredMarkers) {
            if (Test-Path -LiteralPath (Join-Path $versionPath $marker)) {
                return $true
            }
        }
    }

    return $false
}

function Install-WindowsSdk {
    param([string]$SdkVersion)

    $sdkIdVersion = ($SdkVersion -split '\.')[0..2] -join '.'
    $wingetId = "Microsoft.WindowsSDK.$sdkIdVersion"
    Write-Host "Installing Windows SDK via winget: $wingetId"

    & winget install --id $wingetId --accept-package-agreements --accept-source-agreements --silent
    if ($LASTEXITCODE -ne 0) {
        if (Test-UapSdkInstalled -SdkVersion $SdkVersion) {
            Write-Host "winget returned exit code $LASTEXITCODE, but required SDK files are present. Continuing."
            return
        }

        throw "winget install failed for $wingetId with exit code $LASTEXITCODE"
    }
}

if (-not (Test-Path -LiteralPath $projectFile)) {
    throw "Project file not found: $projectFile"
}

$sdkVersion = Get-ProjectTargetPlatformVersion -CsprojPath $projectFile
$sdkIdVersion = ($sdkVersion -split '\.')[0..2] -join '.'
$wingetSdkId = "Microsoft.WindowsSDK.$sdkIdVersion"

if (-not (Test-UapSdkInstalled -SdkVersion $sdkVersion)) {
    if ($InstallMissingSdk) {
        Install-WindowsSdk -SdkVersion $sdkVersion
    }
}

if (-not (Test-UapSdkInstalled -SdkVersion $sdkVersion)) {
    throw "Required Windows SDK UAP targets ($sdkVersion) are missing. Install with: winget install --id $wingetSdkId"
}

$certJson = & $certScript
$certInfo = $certJson | ConvertFrom-Json
if (-not $certInfo.PfxPath) {
    throw 'Certificate bootstrap did not return expected PFX path.'
}

Write-Host "Building MSIX package for $Platform ($Configuration)..."

$msbuildArgs = @(
    'msbuild',
    $projectFile,
    '/restore',
    '/t:Build',
    "/p:Configuration=$Configuration",
    "/p:Platform=$Platform",
    '/p:EnableMsixTooling=true',
    '/p:GenerateAppxPackageOnBuild=true',
    '/p:AppxGeneratePriEnabled=true',
    '/p:AppxGeneratePackageRecipeEnabled=false',
    '/p:UapAppxPackageBuildMode=SideloadOnly',
    '/p:AppxBundle=Never',
    '/p:DebugSymbols=false',
    '/p:DebugType=None',
    '/p:AppxPackageSigningEnabled=true',
    "/p:PackageCertificateKeyFile=$($certInfo.PfxPath)",
    "/p:PackageCertificatePassword=$certificatePasswordPlain",
    "/p:AppxPackageDir=$appxOutputDir"
)

& dotnet @msbuildArgs

if ($LASTEXITCODE -ne 0) {
    throw "MSBuild packaging failed with exit code $LASTEXITCODE"
}

$package = Get-ChildItem -Path $appxOutputDir -Recurse -File |
    Where-Object { $_.Extension -in '.msix', '.appx', '.msixbundle', '.appxbundle' } |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if (-not $package) {
    throw "No package found under: $appxOutputDir"
}

Write-Host 'Stopping extension-related processes before install...'
$processesToStop = @(
    'PowerPlatformNavigator',
    'PowerToys.CommandPalette'
)

foreach ($processName in $processesToStop) {
    Get-Process -Name $processName -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}

Write-Host "Installing package: $($package.FullName)"
try {
    Add-AppxPackage -Path $package.FullName -ForceUpdateFromAnyVersion -ForceApplicationShutdown
}
catch {
    $errorText = $_.ToString()
    if ($errorText -match '0x800B0109') {
        $cerPath = $certInfo.CerPath
        throw "Package signature root trust is missing (0x800B0109). Open an elevated PowerShell once and run: Import-Certificate -FilePath '$cerPath' -CertStoreLocation Cert:\LocalMachine\Root"
    }

    if ($errorText -match '0x80073D02') {
        throw 'Package files are still in use (0x80073D02). Close Command Palette and PowerToys, then rerun the task.'
    }

    if ($errorText -match '0x80073CFB') {
        Write-Host 'Detected same-version package collision (0x80073CFB). Removing existing package and retrying install...'

        $installedPackages = Get-AppxPackage -Name 'PowerPlatformNavigator' -ErrorAction SilentlyContinue
        foreach ($installedPackage in $installedPackages) {
            Remove-AppxPackage -Package $installedPackage.PackageFullName -ErrorAction Stop
        }

        Add-AppxPackage -Path $package.FullName -ForceApplicationShutdown
        Write-Host 'Install retry after package removal succeeded.'
        $errorText = $null
    }

    if (-not [string]::IsNullOrEmpty($errorText)) {
        throw
    }

}

if (-not $SkipCommandPaletteRestart) {
    $processNames = @('PowerToys.CommandPalette')
    foreach ($name in $processNames) {
        Get-Process -Name $name -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    }

    $candidatePaths = @(
        (Join-Path $env:LOCALAPPDATA 'PowerToys\PowerToys.CommandPalette.exe'),
        (Join-Path $env:ProgramFiles 'PowerToys\WinUI3Apps\PowerToys.CommandPalette.exe')
    )

    $started = $false
    foreach ($path in $candidatePaths) {
        if (Test-Path -LiteralPath $path) {
            Start-Process -FilePath $path
            $started = $true
            Write-Host "Restarted Command Palette: $path"
            break
        }
    }

    if (-not $started) {
        Write-Host 'Installed package. Re-open Command Palette to refresh extensions.'
    }
}

Write-Host ''
Write-Host 'Deployment complete.'
Write-Host "Installed package: $($package.FullName)"
