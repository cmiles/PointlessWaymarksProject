using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PointlessWaymarksCmsData.Spatial.Elevation
{
    public static class GoogleElevationService
    {
        public static async Task<double> GetElevation(HttpClient client, string apiKey, double latitude,
            double longitude)
        {
            var elevationReturn = await client.GetStringAsync(
                $"https://maps.googleapis.com/maps/api/elevation/json?locations={latitude},{longitude}&key={apiKey}");

            var elevationParsed = JsonSerializer.Deserialize<GoogleElevationResponse>(elevationReturn);

            return elevationParsed?.Elevations.First().Elevation ?? 0;
        }
    }
}