#ifndef Version
  #define Version = '1902-07-02-00-00-00';
#endif

#ifndef GitCommit
  #define GitCommit = '???';
#endif

#define MyAppPublisher "Charles Miles"
#define MyAppOutputDir "M:\PointlessWaymarksPublications"

#define MyAppDefaultGroupName "Pointless Waymarks"

#define MyAppName "Pointless Waymarks Site Viewer"
#define MyAppDefaultDirName "PointlessWaymarksSiteViewer"
#define MyAppExeName "PointlessWaymarks.SiteViewerGui.exe"
#define MyAppOutputBaseFilename "PointlessWaymarksSiteViewerSetup--"
#define MyAppFilesSource "M:\PointlessWaymarksPublications\PointlessWaymarks.SiteViewerGui\*"

[Setup]
AppId={{03D32A38-B168-4F29-BC00-4115625F458D}
AppName={#MyAppName}
AppVersion={#Version}
AppPublisher={#MyAppPublisher}
WizardStyle=modern
DefaultDirName={autopf}\{#MyAppDefaultDirName}
DefaultGroupName={#MyAppDefaultGroupName}
UninstallDisplayIcon={app}\PointlessWaymarksSiteViewerInstallerIcon.ico
Compression=lzma2
SolidCompression=yes
OutputDir={#MyAppOutputDir}
OutputBaseFilename={#MyAppOutputBaseFilename}{#Version}
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest

[Files]
Source: {#MyAppFilesSource}; DestDir: "{app}\"; Flags: recursesubdirs ignoreversion; AfterInstall:PublishVersionAfterInstall;

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\PointlessWaymarksSiteViewerInstallerIcon.ico"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch application"; Flags: postinstall nowait skipifsilent

[Code]
procedure PublishVersionAfterInstall();
begin
  SaveStringToFile(ExpandConstant('{app}\PublishVersion--{#Version}.txt'), ExpandConstant('({#GitCommit})'), False);
end;