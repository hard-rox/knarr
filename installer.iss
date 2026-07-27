[Setup]
AppName=Knarr
AppVersion=1.0.0
DefaultDirName={autopf}\Knarr
DefaultGroupName=Knarr
Compression=lzma2
SolidCompression=yes
OutputDir=.\publish
OutputBaseFilename=Knarr-Windows-Installer
DisableProgramGroupPage=yes

[Files]
; Point directly to your portable publish directory compiled by GitHub Actions
Source: ".\publish\portable\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Knarr"; Filename: "{app}\Knarr.App.exe"
Name: "{autodesktop}\Knarr"; Filename: "{app}\Knarr.App.exe"
