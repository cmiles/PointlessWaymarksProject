namespace PointlessWaymarksCmsData.Database.PointDetailModels
{
    public class Campground
    {
        public const string DataTypeIdentifier = "Campground";
        public string NotesContentFormat { get; set; }
        public string Notes { get; set; }
        public bool Fee { get; set; }
    }
}