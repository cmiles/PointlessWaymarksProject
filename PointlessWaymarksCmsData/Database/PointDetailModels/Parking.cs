namespace PointlessWaymarksCmsData.Database.PointDetailModels
{
    public class Parking
    {
        public const string DataTypeIdentifier = "Parking";

        public class Parking01
        {
            public string Notes { get; set; }
            public string Version { get; } = "1";
        }
    }
}