param(
    [string]$ProjectFolder = "PowerPlatformNavigator",
    [string]$ExtensionName = "PowerPlatformNavigator",
    [string]$Configuration = "Release",
    [string]$Version = "0.0.1.0",
    [string[]]$Platforms = @("x64", "arm64")
)

$ErrorActionPreference = "Stop"

Write-Host "Building $ExtensionName EXE installer..." -ForegroundColor Green
Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host "Platforms: $($Platforms -join ', ')" -ForegroundColor Yellow

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "..\..")
$ProjectDir = Join-Path $RepoRoot $ProjectFolder
$ProjectFile = Join-Path $ProjectDir "$ExtensionName.csproj"
$SetupTemplatePath = Join-Path $ScriptDir "setup-template.iss"

if (-not (Test-Path $ProjectFile)) {
    throw "Project file not found: $ProjectFile"
}

if (-not (Test-Path $SetupTemplatePath)) {
    throw "Setup template not found: $SetupTemplatePath"
}

Push-Location $ProjectDir
try {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path "$ProjectDir\bin") {
        Remove-Item -Path "$ProjectDir\bin" -Recurse -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path "$ProjectDir\obj") {
        Remove-Item -Path "$ProjectDir\obj" -Recurse -Force -ErrorAction SilentlyContinue
    }

    Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
    dotnet restore $ProjectFile
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE"
    }

    foreach ($Platform in $Platforms) {
        Write-Host "`n=== Building $Platform ===" -ForegroundColor Cyan

        $PublishDir = "$ProjectDir\bin\$Configuration\win-$Platform\publish"
        Write-Host "Publishing self-contained win-$Platform output..." -ForegroundColor Yellow
        dotnet publish $ProjectFile `
            --configuration $Configuration `
            --runtime "win-$Platform" `
            --self-contained true `
            -p:PublishSingleFile=false `
            -p:WindowsPackageType=None `
            --output $PublishDir

        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed for $Platform with exit code $LASTEXITCODE"
        }

        $fileCount = (Get-ChildItem -Path $PublishDir -Recurse -File).Count
        Write-Host "Published $fileCount files to $PublishDir" -ForegroundColor Green

        Write-Host "Preparing Inno Setup script for $Platform..." -ForegroundColor Yellow
        $setupTemplate = Get-Content $SetupTemplatePath -Raw

        $setupScript = $setupTemplate -replace '#define AppVersion ".*"', "#define AppVersion `"$Version`""
        $setupScript = $setupScript -replace 'OutputBaseFilename=(.*?)\{#AppVersion\}', "OutputBaseFilename=`$1{#AppVersion}-$Platform"
        $setupScript = $setupScript -replace 'Source: "bin\\Release\\win-x64\\publish', "Source: `"bin\Release\win-$Platform\publish"

        if ($Platform -eq "arm64") {
            $setupScript = $setupScript -replace '(\[Setup\][^\[]*)(MinVersion=)', "`$1ArchitecturesAllowed=arm64`r`nArchitecturesInstallIn64BitMode=arm64`r`n`$2"
        }
        else {
            $setupScript = $setupScript -replace '(\[Setup\][^\[]*)(MinVersion=)', "`$1ArchitecturesAllowed=x64compatible`r`nArchitecturesInstallIn64BitMode=x64compatible`r`n`$2"
        }

        $SetupScriptPath = "$ProjectDir\setup-$Platform.iss"
        $setupScript | Out-File -FilePath $SetupScriptPath -Encoding UTF8

        Write-Host "Creating $Platform installer with Inno Setup..." -ForegroundColor Yellow
        $InnoSetupPath = "${env:ProgramFiles(x86)}\Inno Setup 6\iscc.exe"
        if (-not (Test-Path $InnoSetupPath)) {
            $InnoSetupPath = "${env:ProgramFiles}\Inno Setup 6\iscc.exe"
        }
        if (-not (Test-Path $InnoSetupPath)) {
            throw "Inno Setup not found at expected locations"
        }

        & $InnoSetupPath $SetupScriptPath
        if ($LASTEXITCODE -ne 0) {
            throw "Inno Setup failed for $Platform with exit code $LASTEXITCODE"
        }

        $installer = Get-ChildItem "$ProjectDir\bin\$Configuration\installer\*-$Platform.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $installer) {
            throw "Installer file not found for $Platform"
        }

        $sizeMB = [math]::Round($installer.Length / 1MB, 2)
        Write-Host "Created $Platform installer: $($installer.Name) ($sizeMB MB)" -ForegroundColor Green
    }

    Write-Host "`nBuild completed successfully." -ForegroundColor Green
}
finally {
    Pop-Location
}
