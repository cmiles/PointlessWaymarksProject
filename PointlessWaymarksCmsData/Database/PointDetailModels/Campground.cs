namespace PointlessWaymarksCmsData.Database.PointDetailModels
{
    public class Campground
    {
        public const string DataTypeIdentifier = "Campground";

        public class Campground01
        {
            public string Notes { get; set; }
            public string Version { get; } = "1";
        }
    }
}