using System.Text.Json;
using PointlessWaymarks.CmsData.Database.Models;

namespace PointlessWaymarks.CmsData.Database.PointDetailDataModels
{
    public static class PointDetailUtilities
    {
        public static string PointDetailHumanReadableType(PointDetail detail)
        {
            return detail.DataType switch
            {
                "Campground" => "Campground",
                "DrivingDirections" => "Driving Directions",
                "Fee" => "Fee",
                "Parking" => "Parking",
                "Peak" => "Peak",
                "Restroom" => "Restroom",
                "TrailJunction" => "Trail Junction",
                "Feature" => JsonSerializer.Deserialize<Feature>(detail.StructuredDataAsJson ?? string.Empty)?.Type ??
                             string.Empty,
                _ => string.Empty
            };
        }

        public static List<string> PointDtoTypeList(PointContentDto toProcess)
        {
            var returnList = new List<string>();

            foreach (var loopDetails in toProcess.PointDetails)
            {
                var detailString = PointDetailHumanReadableType(loopDetails);

                if (!string.IsNullOrWhiteSpace(detailString)) returnList.Add(detailString);
            }

            return returnList;
        }
    }
}