namespace PointlessWaymarksCmsData.Database.PointDetailModels
{
    public class TrailJunction : IPointDetail
    {
        public string DataTypeIdentifier => "Trail Junction";
        public string Notes { get; set; }
        public string NotesContentFormat { get; set; }
        public bool? Sign { get; set; }
    }
}