namespace PointlessWaymarksCmsData.Database.PointDetailDataModels
{
    public class Restroom : IPointDetailData
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