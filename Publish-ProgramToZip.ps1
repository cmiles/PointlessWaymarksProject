$program = $args[0]
$baseName = "PointlessWaymarks.$program"

$ErrorActionPreference = "Stop"

.\Tools-CopyReadmeToProjectSpecificReadme.ps1

$GitCommit = & git rev-parse --short HEAD

dotnet clean .\PointlessWaymarks.sln -property:Configuration=Release -property:Platform=x64 -verbosity:minimal

dotnet restore .\PointlessWaymarks.sln -r win-x64 -verbosity:minimal

$vsWhere = "{0}\Microsoft Visual Studio\Installer\vswhere.exe" -f ${env:ProgramFiles(x86)}

$msBuild = & $vsWhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe

& $msBuild .\PointlessWaymarks.sln -property:Configuration=Release -property:Platform=x64 -verbosity:minimal

if ($lastexitcode -ne 0) {throw ("Exec: " + $errorMessage) }

$publishPath = "M:\PointlessWaymarksPublications\$baseName}}"
if(!(test-path -PathType container $publishPath)) {New-Item -ItemType Directory -Path $publishPath }

Remove-Item -Path $publishPath\* -Recurse

& $msBuild .\$baseName\$baseName.csproj -t:publish -p:PublishProfile=.\$baseName\Properties\PublishProfile\FolderProfile.pubxml -verbosity:minimal

if ($lastexitcode -ne 0) {throw ("Exec: " + $errorMessage) }

$programPublishPath = "M:\PointlessWaymarksPublications\$baseName"
$exePath = "$programPublishPath\$baseName.exe"
$fileVersionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath)

# Calculate hour and minute from FilePrivatePart
$versionHour = [math]::Floor($fileVersionInfo.FilePrivatePart / 100)
$versionMinute = $fileVersionInfo.FilePrivatePart - ($versionHour * 100)
$versionDate = New-Object DateTime($fileVersionInfo.FileMajorPart, $fileVersionInfo.FileMinorPart, $fileVersionInfo.FileBuildPart, $hour, $minute, 0)
$publishVersion = "{0}-{1}-{2}-{3}-{4}" -f $versionDate.ToString("yyyy"), $versionDate.ToString("MM"), $versionDate.ToString("MM"), $versionHour.ToString("00"), $versionMinute.ToString("00")
$destinationZipFile = "M:\PointlessWaymarksPublications\PointlessWaymarks$programZip-{0}.zip" -f $publishVersion

Write-Host "Publish Version: $publishVersion"

Compress-Archive -Path $programPublishPath -DestinationPath $destinationZipFile

Write-Output "Directory '$programPublishPath' has been zipped to '$destinationZipFile'"

if ($lastexitcode -ne 0) {throw ("Exec: " + $errorMessage) }