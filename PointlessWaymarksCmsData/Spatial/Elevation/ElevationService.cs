using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PointlessWaymarksCmsData.Spatial.Elevation
{
    public static class ElevationService
    {
        public static async Task<double?> OpenTopoMapZenElevation(HttpClient client, double latitude,
            double longitude)
        {
            var elevationReturn = await client.GetStringAsync(
                $"https://api.opentopodata.org/v1/mapzen?locations={latitude},{longitude}");

            var elevationParsed = JsonSerializer.Deserialize<ElevationResponse>(elevationReturn);

            return elevationParsed?.Elevations.First().Elevation;
        }

        public static async Task<double?> OpenTopoNedElevation(HttpClient client, double latitude,
            double longitude)
        {
            var elevationReturn = await client.GetStringAsync(
                $"https://api.opentopodata.org/v1/ned10m?locations={latitude},{longitude}");

            var elevationParsed = JsonSerializer.Deserialize<ElevationResponse>(elevationReturn);

            return elevationParsed?.Elevations.First().Elevation;
        }
    }
}