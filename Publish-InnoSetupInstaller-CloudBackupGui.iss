#ifndef Version
  #define Version = '1902-07-02-00-00-00';
#endif

#ifndef GitCommit
  #define GitCommit = '???';
#endif

#define MyAppPublisher "Charles Miles"
#define MyAppOutputDir "M:\PointlessWaymarksPublications"

#define MyAppDefaultGroupName "Pointless Waymarks"

#define MyAppName "Pointless Waymarks Cloud Backup"
#define MyAppDefaultDirName "PointlessWaymarksCloudBackup"
#define MyAppExeName "PointlessWaymarks.CloudBackupGui.exe"
#define MyAppOutputBaseFilename "PointlessWaymarks-CloudBackupGui-Setup--"
#define MyAppFilesSource "M:\PointlessWaymarksPublications\PointlessWaymarks.CloudBackupGui\*"

[Setup]
AppId={{FD297C57-ABFF-4824-98B5-31F6A5B6026F}
AppName={#MyAppName}
AppVersion={#Version}
AppPublisher={#MyAppPublisher}
WizardStyle=modern
DefaultDirName={autopf}\{#MyAppDefaultDirName}
DefaultGroupName={#MyAppDefaultGroupName}
Compression=lzma2
SolidCompression=yes
OutputDir={#MyAppOutputDir}
OutputBaseFilename={#MyAppOutputBaseFilename}{#Version}
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
WizardSmallImageFile="M:\PointlessWaymarksPublications\PointlessWaymarks.CloudBackupGui\CloudBackupInstallerTopRightImage.bmp"
WizardImageFile="M:\PointlessWaymarksPublications\PointlessWaymarks.CloudBackupGui\CloudBackupInstallerLeftImage.bmp"

[Files]
Source: {#MyAppFilesSource}; DestDir: "{app}\"; Flags: recursesubdirs ignoreversion; AfterInstall:PublishVersionAfterInstall;

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}";

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch application"; Flags: postinstall nowait skipifsilent

[Code]
procedure PublishVersionAfterInstall();
begin
  SaveStringToFile(ExpandConstant('{app}\PublishVersion--{#Version}.txt'), ExpandConstant('({#GitCommit})'), False);
end;