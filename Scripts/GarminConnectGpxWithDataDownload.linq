<Query Kind="Statements">
  <NuGetReference>NetTopologySuite.IO.GPX</NuGetReference>
  <NuGetReference>Unofficial.Garmin.Connect</NuGetReference>
  <Namespace>Garmin.Connect</Namespace>
  <Namespace>Garmin.Connect.Auth</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>NetTopologySuite.IO</Namespace>
</Query>

var login = ""; //Connect Username
var password = ""; //Connect Password
var downloadDirectory = ""; //@"C:\GarminDownload"

var authParameters = new BasicAuthParameters(login, password);

var referenceDate = DateTime.Now;

var client = new GarminConnectClient(new GarminConnectContext(new HttpClient(), authParameters));

for (int i = 0; i < 30; i++)
{
	var targetStart = new DateTime(referenceDate.AddYears(-i).Year, 1, 1);
	var targetEnd = new DateTime(referenceDate.AddYears(-i).Year, 12, 31);

	var activityList = await client.GetActivitiesByDate(targetStart, targetEnd, string.Empty);

	foreach (var loopActivity in activityList.Where(x => x.HasPolyline))
	{
		var file = await client.DownloadActivity(loopActivity.ActivityId, Garmin.Connect.Models.ActivityDownloadFormat.GPX);
		await File.WriteAllBytesAsync($@"{downloadDirectory}\{loopActivity.ActivityId}.gpx", file);
	}
}

var directory = new DirectoryInfo(downloadDirectory);
var files = directory.EnumerateFiles("*.gpx").ToList();

foreach (var loopFile in files)
{
	var gpxFile = GpxFile.Parse(await File.ReadAllTextAsync(loopFile.FullName),
	new GpxReaderSettings
	{
		BuildWebLinksForVeryLongUriValues = true,
		IgnoreBadDateTime = true,
		IgnoreUnexpectedChildrenOfTopLevelElement = true,
		IgnoreVersionAttribute = true
	});

	if (gpxFile.Tracks.Any(t => t.Segments.All(y => y.Waypoints.Count() == 0)))
	{
		$"{loopFile.FullName} - {gpxFile.Tracks.Count()} {gpxFile.Tracks.First().Segments.Count()}".Dump();
		loopFile.Delete();
	}
}


