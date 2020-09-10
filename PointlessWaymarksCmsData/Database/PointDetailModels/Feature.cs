namespace PointlessWaymarksCmsData.Database.PointDetailModels
{
    public class Feature : IPointDetail
    {
        public string DataTypeIdentifier => "Feature";
        public string Notes { get; set; }
        public string NotesContentFormat { get; set; }
        public string Title { get; set; }
    }
}