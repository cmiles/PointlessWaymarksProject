$ErrorActionPreference = "Stop"

$PublishVersion = get-date -f yyyy-MM-dd-HH-mm

$GitCommit = & git rev-parse --short HEAD

dotnet clean .\PointlessWaymarks.sln -property:Configuration=Release -property:Platform=x64 -verbosity:minimal

dotnet restore .\PointlessWaymarks.sln -r win10-x64 -verbosity:minimal

$vsWhere = "{0}\Microsoft Visual Studio\Installer\vswhere.exe" -f ${env:ProgramFiles(x86)}

$msBuild = & $vsWhere -latest -prerelease -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe

& $msBuild .\PointlessWaymarks.sln -property:Configuration=Release -property:Platform=x64 -verbosity:minimal

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }


$publishPath = "M:\PointlessWaymarksPublications\PointlessWaymarks.Task.GarminConnectGpxImport"
if(!(test-path -PathType container $publishPath)) { New-Item -ItemType Directory -Path $publishPath }

Remove-Item -Path $publishPath\* -Recurse

& $msBuild .\PointlessWaymarks.Task.GarminConnectGpxImport\PointlessWaymarks.Task.GarminConnectGpxImport.csproj -t:publish -p:PublishProfile=.\PointlessWaymarks.GeoToolsGui\Properties\PublishProfile\FolderProfile.pubxml -verbosity:minimal

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }


$publishPath = "M:\PointlessWaymarksPublications\PointlessWaymarks.Task.MemoriesEmail"
if(!(test-path -PathType container $publishPath)) { New-Item -ItemType Directory -Path $publishPath }

Remove-Item -Path $publishPath\* -Recurse

& $msBuild .\PointlessWaymarks.Task.MemoriesEmail\PointlessWaymarks.Task.MemoriesEmail.csproj -t:publish -p:PublishProfile=.\PointlessWaymarks.Task.MemoriesEmail\Properties\PublishProfile\FolderProfile.pubxml -verbosity:minimal

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }


$publishPath = "M:\PointlessWaymarksPublications\PointlessWaymarks.Task.PhotoPickup"
if(!(test-path -PathType container $publishPath)) { New-Item -ItemType Directory -Path $publishPath }

Remove-Item -Path $publishPath\* -Recurse

& $msBuild .\PointlessWaymarks.Task.PhotoPickup\PointlessWaymarks.Task.PhotoPickup.csproj -t:publish -p:PublishProfile=.\PointlessWaymarks.Task.PhotoPickup\Properties\PublishProfile\FolderProfile.pubxml -verbosity:minimal

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }


$publishPath = "M:\PointlessWaymarksPublications\PointlessWaymarks.Task.PublishSiteToAmazonS3"
if(!(test-path -PathType container $publishPath)) { New-Item -ItemType Directory -Path $publishPath }

Remove-Item -Path $publishPath\* -Recurse

& $msBuild .\PointlessWaymarks.Task.PublishSiteToAmazonS3\PointlessWaymarks.Task.PublishSiteToAmazonS3.csproj -t:publish -p:PublishProfile=.\PointlessWaymarks.Task.PublishSiteToAmazonS3\Properties\PublishProfile\FolderProfile.pubxml -verbosity:minimal

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }


$publishPath = "M:\PointlessWaymarksPublications\PointlessWaymarks.CloudBackupRunner"
if(!(test-path -PathType container $publishPath)) { New-Item -ItemType Directory -Path $publishPath }

Remove-Item -Path $publishPath\* -Recurse

& $msBuild .\PointlessWaymarks.CloudBackupRunner\PointlessWaymarks.CloudBackupRunner.csproj -t:publish -p:PublishProfile=.\PointlessWaymarks.CloudBackupRunner\Properties\PublishProfile\FolderProfile.pubxml -verbosity:minimal

if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }
