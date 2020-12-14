using System.Collections.Generic;
using System.Text.Json;
using PointlessWaymarksCmsData.Database.Models;

namespace PointlessWaymarksCmsData.Database.PointDetailDataModels
{
    public static class PointDetailUtilities
    {
        public static string PointDetailHumanReadableType(PointDetail detail)
        {
            return detail.DataType switch
            {
                "Campground" => "Campground",
                "Parking" => "Parking",
                "Peak" => "Peak",
                "Restroom" => "Restroom",
                "TrailJunction" => "Trail Junction",
                "Feature" => JsonSerializer.Deserialize<Feature>(detail.StructuredDataAsJson)?.Type ?? string.Empty,
                _ => string.Empty
            };
        }

        public static List<string> PointDtoTypeList(PointContentDto toProcess)
        {
            var returnList = new List<string>();

            foreach (var loopDetails in toProcess.PointDetails ?? new List<PointDetail>())
            {
                var detailString = PointDetailHumanReadableType(loopDetails);

                if (!string.IsNullOrWhiteSpace(detailString)) returnList.Add(detailString);
            }

            return returnList;
        }
    }
}