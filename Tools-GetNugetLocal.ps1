$sourceNugetExe = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$nugetExe = ".\nuget.exe"
Invoke-WebRequest $sourceNugetExe -OutFile $nugetExe