namespace PointlessWaymarksCmsData.Database.PointDetailModels
{
    public class Parking : IPointDetail
    {
        public string DataTypeIdentifier => "Trail Junction";
        public bool Fee { get; set; }
        public string Notes { get; set; }
        public string NotesContentFormat { get; set; }
    }
}