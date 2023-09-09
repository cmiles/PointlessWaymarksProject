$ErrorActionPreference = "Stop"

if (-not ("BuildTools\PointlessWaymarks.PublishReadmeHelper.exe" | Test-Path)) {
	
	$vsWhere = "{0}\Microsoft Visual Studio\Installer\vswhere.exe" -f ${env:ProgramFiles(x86)}

	$msBuild = & $vsWhere -latest -prerelease -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe

	& $msBuild .\PointlessWaymarks.PublishReadmeHelper\PointlessWaymarks.PublishReadmeHelper.csproj -t:publish -p:PublishProfile=.\PointlessWaymarks.PublishReadmeHelper\Properties\PublishProfile\FolderProfile.pubxml -verbosity:minimal

	if ($lastexitcode -ne 0) { throw ("Exec: " + $errorMessage) }
	
}

C:\Code\PointlessWaymarksProject-05\BuildTools\PointlessWaymarks.PublishReadmeHelper.exe