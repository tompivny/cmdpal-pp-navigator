[CmdletBinding()]
param(
    [string]$Subject = 'CN=PowerPlatformNavigator Dev',
    [string]$OutputDirectory = "$PSScriptRoot\\..\\.certs",
    [string]$PfxName = 'PowerPlatformNavigator.DevCert.pfx',
    [string]$CerName = 'PowerPlatformNavigator.DevCert.cer'
)

$ErrorActionPreference = 'Stop'

function Test-IsAdministrator {
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
if (-not (Test-Path -LiteralPath $resolvedOutput)) {
    New-Item -ItemType Directory -Path $resolvedOutput | Out-Null
}

$pfxPath = Join-Path $resolvedOutput $PfxName
$cerPath = Join-Path $resolvedOutput $CerName

$passwordPlain = if ($env:CMDPAL_DEV_CERT_PASSWORD) { $env:CMDPAL_DEV_CERT_PASSWORD } else { 'cmdpaldev' }
$pfxPassword = ConvertTo-SecureString -String $passwordPlain -AsPlainText -Force

$existing = Get-ChildItem -Path Cert:\CurrentUser\My |
    Where-Object { $_.Subject -eq $Subject -and $_.HasPrivateKey } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if ($existing) {
    $tempExportPath = Join-Path $env:TEMP ([System.Guid]::NewGuid().ToString() + '.pfx')
    try {
        Export-PfxCertificate -Cert "Cert:\CurrentUser\My\$($existing.Thumbprint)" -FilePath $tempExportPath -Password $pfxPassword -Force | Out-Null
    }
    catch {
        Write-Host 'Existing certificate private key is not exportable. Creating a new exportable certificate.'
        $existing = $null
    }
    finally {
        if (Test-Path -LiteralPath $tempExportPath) {
            Remove-Item -LiteralPath $tempExportPath -Force
        }
    }
}

if (-not $existing) {
    Write-Host "Creating new development code-signing certificate: $Subject"
    $newCertParams = @{
        Type = 'CodeSigningCert'
        Subject = $Subject
        CertStoreLocation = 'Cert:\CurrentUser\My'
        NotAfter = (Get-Date).AddYears(3)
        KeyExportPolicy = 'Exportable'
    }

    $existing = New-SelfSignedCertificate @newCertParams
}
else {
    Write-Host "Using existing development certificate: $($existing.Thumbprint)"
}

Export-PfxCertificate -Cert "Cert:\CurrentUser\My\$($existing.Thumbprint)" -FilePath $pfxPath -Password $pfxPassword -Force | Out-Null
Export-Certificate -Cert "Cert:\CurrentUser\My\$($existing.Thumbprint)" -FilePath $cerPath -Type CERT -Force | Out-Null

$trustedPeoplePath = 'Cert:\CurrentUser\TrustedPeople'
$alreadyTrusted = Get-ChildItem -Path $trustedPeoplePath |
    Where-Object { $_.Thumbprint -eq $existing.Thumbprint } |
    Select-Object -First 1

if (-not $alreadyTrusted) {
    Import-Certificate -FilePath $cerPath -CertStoreLocation $trustedPeoplePath | Out-Null
    Write-Host 'Imported certificate into CurrentUser\TrustedPeople.'
}

$trustedRootPath = 'Cert:\CurrentUser\Root'
$alreadyRootTrusted = Get-ChildItem -Path $trustedRootPath |
    Where-Object { $_.Thumbprint -eq $existing.Thumbprint } |
    Select-Object -First 1

if (-not $alreadyRootTrusted) {
    Import-Certificate -FilePath $cerPath -CertStoreLocation $trustedRootPath | Out-Null
    Write-Host 'Imported certificate into CurrentUser\Root.'
}

if (Test-IsAdministrator) {
    $machineRootPath = 'Cert:\LocalMachine\Root'
    $alreadyMachineRootTrusted = Get-ChildItem -Path $machineRootPath |
        Where-Object { $_.Thumbprint -eq $existing.Thumbprint } |
        Select-Object -First 1

    if (-not $alreadyMachineRootTrusted) {
        Import-Certificate -FilePath $cerPath -CertStoreLocation $machineRootPath | Out-Null
        Write-Host 'Imported certificate into LocalMachine\Root.'
    }
}
else {
    Write-Host 'Not elevated: skipped LocalMachine\Root trust import.'
}

$result = [PSCustomObject]@{
    Subject = $Subject
    Thumbprint = $existing.Thumbprint
    PfxPath = $pfxPath
    CerPath = $cerPath
}

$result | ConvertTo-Json -Compress
