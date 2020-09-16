namespace PointlessWaymarksCmsData.Database.PointDetailModels
{
    public class Restroom : IPointDetail
    {
        public string DataTypeIdentifier => "Restroom";
        public string Notes { get; set; }
        public string NotesContentFormat { get; set; }

        public (bool isValid, string validationMessage) Validate()
        {
            return (true, string.Empty);
        }
    }
}