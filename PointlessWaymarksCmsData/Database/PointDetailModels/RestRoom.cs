namespace PointlessWaymarksCmsData.Database.PointDetailModels
{
    public class RestRoom : IPointDetail
    {
        public string DataTypeIdentifier => "Rest Room";
        public string Notes { get; set; }
        public string NotesContentFormat { get; set; }
    }
}