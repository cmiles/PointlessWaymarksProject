namespace PointlessWaymarksCmsData.Database.PointDetailModels
{
    public class Peak
    {
        public const string DataTypeIdentifier = "Peak";

        public class Peak01
        {
            public string Notes { get; set; }
            public string Version { get; } = "1";
        }
    }
}