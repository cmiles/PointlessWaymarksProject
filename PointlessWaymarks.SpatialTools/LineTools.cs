using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace PointlessWaymarks.SpatialTools;

public static class LineTools
{
    public static List<CoordinateZ> CoordinateListFromGeoJsonFeatureCollectionWithLinestring(string geoJson)
    {
        if (string.IsNullOrWhiteSpace(geoJson)) return [];

        var featureCollection = GeoJsonTools.DeserializeStringToFeatureCollection(geoJson);

        if (featureCollection.Count < 1) return [];

        var possibleLine = featureCollection.FirstOrDefault(x => x.Geometry is LineString);

        if (possibleLine == null) return [];

        var geoLine = (LineString)possibleLine.Geometry;

        return geoLine.Coordinates.Select(x => new CoordinateZ(x.X, x.Y, x.Z)).ToList();
    }

    public static List<LineElevationChartDataPoint> ElevationChartData(List<CoordinateZ> lineCoordinates)
    {
        if (lineCoordinates.Count == 0) return [];

        var returnList = new List<LineElevationChartDataPoint>
            { new(0, lineCoordinates[0].Z, 0, 0, lineCoordinates[0].Y, lineCoordinates[0].X) };

        if (lineCoordinates.Count == 1) return returnList;

        var accumulatedDistance = 0D;
        var accumulatedClimb = 0D;
        var accumulatedDescent = 0D;

        for (var i = 1; i < lineCoordinates.Count; i++)
        {
            var elevationChange = lineCoordinates[i - 1].Z - lineCoordinates[i].Z;
            switch (elevationChange)
            {
                case > 0:
                    accumulatedClimb += elevationChange;
                    break;
                case < 0:
                    accumulatedDescent += elevationChange;
                    break;
            }
            
            accumulatedDistance += DistanceTools.GetDistanceInMeters(lineCoordinates[i - 1].X, lineCoordinates[i - 1].Y,
                lineCoordinates[i].X, lineCoordinates[i].Y);

            returnList.Add(new LineElevationChartDataPoint(accumulatedDistance, lineCoordinates[i].Z, accumulatedClimb,
                accumulatedDescent, lineCoordinates[i].Y, lineCoordinates[i].X));
        }

        return returnList;
    }

    public static List<LineElevationChartDataPoint> ElevationChartDataFromGeoJsonFeatureCollectionWithLinestring(
        string geoJson)
    {
        return ElevationChartData(CoordinateListFromGeoJsonFeatureCollectionWithLinestring(geoJson));
    }


    public static async Task<string> GeoJsonWithLineStringFromCoordinateList(List<CoordinateZ> pointList,
        bool replaceElevations, IProgress<string>? progress = null)
    {
        if (replaceElevations)
            await ElevationService.OpenTopoMapZenElevation(pointList, progress)
                .ConfigureAwait(false);

        // ReSharper disable once CoVariantArrayConversion It appears from testing that a linestring will reflect CoordinateZ
        var newLineString = new LineString(pointList.ToArray());
        var newFeature = new Feature(newLineString, new AttributesTable());
        var featureCollection = new FeatureCollection { newFeature };

        return await GeoJsonTools.SerializeFeatureCollectionToGeoJson(featureCollection);
    }
}