; Inno Setup script for Power Platform Navigator Command Palette extension
#define AppVersion "0.0.1.0"

[Setup]
AppId={{1c7dd239-8f44-4469-8f0d-8f8618d3f1d2}}
AppName=Power Platform Navigator
AppVersion={#AppVersion}
AppPublisher=A Lone Developer
DefaultDirName={autopf}\PowerPlatformNavigator
OutputDir=bin\Release\installer
OutputBaseFilename=PowerPlatformNavigator-Setup-{#AppVersion}
Compression=lzma
SolidCompression=yes
MinVersion=10.0.19041

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "bin\Release\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Classes\CLSID\{{0ae7b923-8f24-4652-927c-a609143a2d62}}"; ValueData: "PowerPlatformNavigator"
Root: HKCU; Subkey: "SOFTWARE\Classes\CLSID\{{0ae7b923-8f24-4652-927c-a609143a2d62}}\LocalServer32"; ValueData: "{app}\PowerPlatformNavigator.exe -RegisterProcessAsComServer"
