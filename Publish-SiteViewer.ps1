$ErrorActionPreference = "Stop"

$PublishVersion = get-date -f yyyy-MM-dd-HH-mm

$GitCommit = & git rev-parse --short HEAD

dotnet clean .\PointlessWaymarks.sln -property:Configuration=Release -property:Platform=x64 -verbosity:minimal

dotnet restore .\PointlessWaymarks.sln -r win10-x64 -verbosity:minimal

$vsWhere = "{0}\Microsoft Visual Studio\Installer\vswhere.exe" -f ${env:ProgramFiles(x86)}

$msBuild = & $vsWhere -latest -prerelease -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe

& $msBuild .\PointlessWaymarks.sln -property:Configuration=Release -property:Platform=x64 -verbosity:minimal

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }

$publishPath = "M:\PointlessWaymarksPublications\PointlessWaymarks.SiteViewerGui"
if(!(test-path -PathType container $publishPath)) { New-Item -ItemType Directory -Path $publishPath }

& $msBuild .\PointlessWaymarks.SiteViewerGui\PointlessWaymarks.SiteViewerGui.csproj -t:publish -p:PublishProfile=.\PointlessWaymarks.SiteViewerGui\Properties\PublishProfile\FolderProfile.pubxml -verbosity:minimal

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }

& 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe' .\Publish-InnoSetupInstaller-SiteViewer.iss /DVersion=$PublishVersion /DGitCommit=$GitCommit

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }

