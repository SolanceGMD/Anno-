#define MyAppName "anno+"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "SolanceGMD"
#define MyAppExeName "anno+.exe"

[Setup]
AppId={{A6E57D82-2D39-4CF6-BD4E-123456789001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\anno+
DefaultGroupName=anno+
OutputDir=..\dist
OutputBaseFilename=anno+-setup
SetupIconFile=
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "..\publish\anno+.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\anno+"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\anno+"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch anno+"; Flags: nowait postinstall skipifsilent
