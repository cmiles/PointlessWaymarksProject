using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PointlessWaymarksCmsData.Spatial.Elevation
{
    public static class ElevationService
    {
        public static async Task<double?> OpenTopoMapZenElevation(HttpClient client, double latitude, double longitude,
            IProgress<string> progress)
        {
            var requestUri = $"https://api.opentopodata.org/v1/mapzen?locations={latitude},{longitude}";

            progress?.Report($"Sending request to {requestUri}");

            var elevationReturn = await client.GetStringAsync(requestUri);

            progress?.Report($"Parsing Return from {requestUri}");

            var elevationParsed = JsonSerializer.Deserialize<ElevationResponse>(elevationReturn);

            return elevationParsed?.Elevations.First().Elevation;
        }

        public static async Task<double?> OpenTopoNedElevation(HttpClient client, double latitude, double longitude,
            IProgress<string> progress)
        {
            var requestUri = $"https://api.opentopodata.org/v1/ned10m?locations={latitude},{longitude}";

            progress?.Report($"Sending request to {requestUri}");

            var elevationReturn = await client.GetStringAsync(requestUri);

            progress?.Report($"Parsing Return from {requestUri}");

            var elevationParsed = JsonSerializer.Deserialize<ElevationResponse>(elevationReturn);

            return elevationParsed?.Elevations.First().Elevation;
        }
    }
}