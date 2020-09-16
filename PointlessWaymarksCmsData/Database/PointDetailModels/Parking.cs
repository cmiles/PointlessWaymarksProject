namespace PointlessWaymarksCmsData.Database.PointDetailModels
{
    public class Parking : IPointDetail
    {
        public string DataTypeIdentifier => "Parking";
        public bool? Fee { get; set; }
        public string Notes { get; set; }
        public string NotesContentFormat { get; set; }

        public (bool isValid, string validationMessage) Validate()
        {
            return (true, string.Empty);
        }
    }
}