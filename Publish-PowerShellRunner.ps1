$ErrorActionPreference = "Stop"

.\Tools-CopyReadmeToProjectSpecificReadme.ps1

$PublishVersion = get-date -f yyyy-MM-dd-HH-mm

$GitCommit = & git rev-parse --short HEAD

dotnet clean .\PointlessWaymarks.sln -property:Configuration=Release -property:Platform=x64 -verbosity:minimal

dotnet restore .\PointlessWaymarks.sln -r win-x64 -verbosity:minimal

$vsWhere = "{0}\Microsoft Visual Studio\Installer\vswhere.exe" -f ${env:ProgramFiles(x86)}

$msBuild = & $vsWhere -latest -prerelease -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe

& $msBuild .\PointlessWaymarks.sln -property:Configuration=Release -property:Platform=x64 -verbosity:minimal

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }

$publishPath = "M:\PointlessWaymarksPublications\PointlessWaymarks.PowerShellRunnerGui"
if(!(test-path -PathType container $publishPath)) { New-Item -ItemType Directory -Path $publishPath }

Remove-Item -Path $publishPath\* -Recurse

& $msBuild .\PointlessWaymarks.PowerShellRunnerGui\PointlessWaymarks.PowerShellRunnerGui.csproj -t:publish -p:PublishProfile=.\PointlessWaymarks.PowerShellRunnerGui\Properties\PublishProfile\FolderProfile.pubxml -verbosity:minimal

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }

& 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe' .\Publish-InnoSetupInstaller-PowerShellRunner.iss /DVersion=$PublishVersion /DGitCommit=$GitCommit

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }


