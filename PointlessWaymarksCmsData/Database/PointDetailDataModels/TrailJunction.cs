namespace PointlessWaymarksCmsData.Database.PointDetailDataModels
{
    public class TrailJunction : IPointDetailData
    {
        public string DataTypeIdentifier => "Trail Junction";
        public string Notes { get; set; }
        public string NotesContentFormat { get; set; }
        public bool? Sign { get; set; }

        public (bool isValid, string validationMessage) Validate()
        {
            return (true, string.Empty);
        }
    }
}