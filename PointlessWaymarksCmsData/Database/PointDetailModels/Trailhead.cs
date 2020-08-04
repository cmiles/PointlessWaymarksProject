namespace PointlessWaymarksCmsData.Database.PointDetailModels
{
    public class Trailhead
    {
        public const string DataTypeIdentifier = "Trailhead";

        public class Trailhead01
        {
            public string Notes { get; set; }
            public string Version { get; } = "1";
        }
    }
}