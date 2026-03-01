# VS Code local workflow (no Visual Studio)

This project can be packaged and sideloaded for PowerToys Command Palette directly from VS Code.

## One-time prerequisites

1. Install the required Windows SDK (matching project target `10.0.26100.0`):

   ```powershell
   winget install --id Microsoft.WindowsSDK.10.0.26100
   ```

2. Trust the local dev signing cert in machine root store (one-time, elevated PowerShell):

   ```powershell
   Import-Certificate -FilePath .\.certs\PowerPlatformNavigator.DevCert.cer -CertStoreLocation Cert:\LocalMachine\Root
   ```

3. Make sure PowerToys Command Palette is installed and can run on this machine.

## One-command deploy loop

Use the default VS Code build task:

- **Task name:** `CmdPal: Deploy extension (x64)`
- **Shortcut:** `Ctrl+Shift+B`

That task runs:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/Deploy-CommandPaletteExtension.ps1 -Platform x64 -Configuration Debug -InstallMissingSdk
```

The script does all of the following:

- Creates/reuses a local self-signed code-signing cert (`CN=PowerPlatformNavigator Dev`)
- Exports cert files under `.certs/`
- Builds a signed MSIX package
- Installs/updates the package via `Add-AppxPackage`
- Attempts to restart Command Palette so changes appear immediately

## Notes

- Package publisher in `Package.appxmanifest` is set to `CN=PowerPlatformNavigator Dev` for local sideload.
- If SDK installation is blocked by permissions/UAC in terminal, run the `winget install` command manually in an elevated terminal once, then rerun the VS Code task.
- You can override cert password by setting environment variable `CMDPAL_DEV_CERT_PASSWORD`.
