namespace PointlessWaymarksCmsData.Database.PointDetailModels
{
    public class Campground : IPointDetail
    {
        public string DataTypeIdentifier => "Campground";

        public bool? Fee { get; set; }
        public string Notes { get; set; }
        public string NotesContentFormat { get; set; }

        public (bool isValid, string validationMessage) Validate()
        {
            return (true, string.Empty);
        }
    }
}