namespace PointlessWaymarksCmsData.Database.PointDetailModels
{
    public class TrailJunction : IPointDetail
    {
        public string DataTypeIdentifier => "TrailJunction";
        public string Notes { get; set; }
        public string NotesContentFormat { get; set; }
        public bool Sign { get; set; }
    }
}