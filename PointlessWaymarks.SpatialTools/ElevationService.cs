using System.Data;
using System.Text.Json;
using NetTopologySuite.Geometries;
using PointlessWaymarks.SpatialTools.ElevationServiceModels;
using Serilog;

namespace PointlessWaymarks.SpatialTools;

public static class ElevationService
{
    private static readonly HttpClient ElevationHttpClient = new();

    private static async Task<List<CoordinateZ>> OpenTopoElevation(string openTopoDataSet,
        List<CoordinateZ> coordinates, IProgress<string>? progress)
    {
        if (!coordinates.Any()) return coordinates;

        var partitionedCoordinates = coordinates.Chunk(100).ToList();

        var resultList = new List<ElevationResult>();

        progress?.Report(
            $"{coordinates.Count} Coordinates for Elevation - querying in {partitionedCoordinates.Count} groups...");

        var isFirst = true;
        
        foreach (var loopCoordinateGroups in partitionedCoordinates)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                await Task.Delay(2000);
            }
            
            var requestUri =
                $"https://api.opentopodata.org/v1/{openTopoDataSet}?locations={string.Join("|", loopCoordinateGroups.Select(x => $"{x.Y},{x.X}"))}";

            progress?.Report($"Sending request to {requestUri}");

            var elevationReturn = await ElevationHttpClient.GetStringAsync(requestUri).ConfigureAwait(false);

            progress?.Report($"Parsing Return from {requestUri}");

            var elevationParsed = JsonSerializer.Deserialize<ElevationResponse>(elevationReturn);

            if (elevationParsed == null)
            {
                Log.Error("Elevation Service - Could not parse information from the Elevation Service - Uri {0}",
                    requestUri);
                throw new DataException(
                    $"Elevation Service - Could not parse information from the Elevation Service - Uri {requestUri}");
            }

            resultList.AddRange(elevationParsed.Elevations);
        }

        progress?.Report("Assigning results to Coordinates");

        foreach (var loopResults in resultList)
        {
            if (loopResults.Location == null) continue;
            // ReSharper disable CompareOfFloatsByEqualityOperator
            //Expecting to get the exact Lat Long back thru the elevation query
            coordinates.Where(x => x.X == loopResults.Location.Longitude && x.Y == loopResults.Location.Latitude)
                // ReSharper restore CompareOfFloatsByEqualityOperator
                .ToList().ForEach(x => x.Z = loopResults.Elevation ?? 0);
        }

        return coordinates;
    }

    private static async Task<double?> OpenTopoElevation(string openTopoDataSet, double latitude, double longitude,
        IProgress<string>? progress)
    {
        var requestUri = $"https://api.opentopodata.org/v1/{openTopoDataSet}?locations={latitude},{longitude}";

        progress?.Report($"Sending request to {requestUri}");

        var elevationReturn = await ElevationHttpClient.GetStringAsync(requestUri).ConfigureAwait(false);

        progress?.Report($"Parsing Return from {requestUri}");

        var elevationParsed = JsonSerializer.Deserialize<ElevationResponse>(elevationReturn);

        return elevationParsed?.Elevations.First().Elevation;
    }

    public static async Task<List<CoordinateZ>> OpenTopoMapZenElevation(List<CoordinateZ> coordinates,
        IProgress<string>? progress)
    {
        return await OpenTopoElevation("mapzen", coordinates, progress).ConfigureAwait(false);
    }

    public static async Task<double?> OpenTopoMapZenElevation(double latitude, double longitude,
        IProgress<string>? progress)
    {
        return await OpenTopoElevation("mapzen", latitude, longitude, progress).ConfigureAwait(false);
    }

    public static async Task<List<CoordinateZ>> OpenTopoNedElevation(List<CoordinateZ> coordinates,
        IProgress<string>? progress)
    {
        return await OpenTopoElevation("ned10m", coordinates, progress).ConfigureAwait(false);
    }

    public static async Task<double?> OpenTopoNedElevation(double latitude, double longitude,
        IProgress<string>? progress)
    {
        return await OpenTopoElevation("ned10m", latitude, longitude, progress).ConfigureAwait(false);
    }
}