using PointlessWaymarksCmsData.Content;

namespace PointlessWaymarksCmsData.Database.PointDetailModels
{
    public class Feature : IPointDetail
    {
        public string DataTypeIdentifier => "Feature";
        public string Notes { get; set; }
        public string NotesContentFormat { get; set; }
        public string Title { get; set; }

        public (bool isValid, string validationMessage) Validate()
        {
            return CommonContentValidation.ValidateTitle(Title);
        }
    }
}