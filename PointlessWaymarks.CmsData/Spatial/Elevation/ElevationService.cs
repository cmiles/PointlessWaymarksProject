﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using Serilog;

namespace PointlessWaymarks.CmsData.Spatial.Elevation
{
    public static class ElevationService
    {
        private static async Task<List<CoordinateZ>> OpenTopoElevation(HttpClient client, string openTopoDataSet,
            List<CoordinateZ> coordinates, IProgress<string>? progress)
        {
            if (!coordinates.Any()) return coordinates;

            var partitionedCoordinates = coordinates.Chunk(100).ToList();

            var resultList = new List<ElevationResult>();

            progress?.Report(
                $"{coordinates.Count} Coordinates for Elevation - querying in {partitionedCoordinates.Count} groups...");

            foreach (var loopCoordinateGroups in partitionedCoordinates)
            {
                var requestUri =
                    $"https://api.opentopodata.org/v1/{openTopoDataSet}?locations={string.Join("|", loopCoordinateGroups.Select(x => $"{x.Y},{x.X}"))}";

                progress?.Report($"Sending request to {requestUri}");

                var elevationReturn = await client.GetStringAsync(requestUri).ConfigureAwait(false);

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

        private static async Task<double?> OpenTopoElevation(HttpClient client, string openTopoDataSet, double latitude,
            double longitude, IProgress<string>? progress)
        {
            var requestUri = $"https://api.opentopodata.org/v1/{openTopoDataSet}?locations={latitude},{longitude}";

            progress?.Report($"Sending request to {requestUri}");

            var elevationReturn = await client.GetStringAsync(requestUri).ConfigureAwait(false);

            progress?.Report($"Parsing Return from {requestUri}");

            var elevationParsed = JsonSerializer.Deserialize<ElevationResponse>(elevationReturn);

            return elevationParsed?.Elevations.First().Elevation;
        }

        public static async Task<List<CoordinateZ>> OpenTopoMapZenElevation(HttpClient client,
            List<CoordinateZ> coordinates, IProgress<string>? progress)
        {
            return await OpenTopoElevation(client, "mapzen", coordinates, progress).ConfigureAwait(false);
        }

        public static async Task<double?> OpenTopoMapZenElevation(HttpClient client, double latitude, double longitude,
            IProgress<string>? progress)
        {
            return await OpenTopoElevation(client, "mapzen", latitude, longitude, progress).ConfigureAwait(false);
        }

        public static async Task<List<CoordinateZ>> OpenTopoNedElevation(HttpClient client,
            List<CoordinateZ> coordinates, IProgress<string>? progress)
        {
            return await OpenTopoElevation(client, "ned10m", coordinates, progress).ConfigureAwait(false);
        }

        public static async Task<double?> OpenTopoNedElevation(HttpClient client, double latitude, double longitude,
            IProgress<string>? progress)
        {
            return await OpenTopoElevation(client, "ned10m", latitude, longitude, progress).ConfigureAwait(false);
        }
    }
}