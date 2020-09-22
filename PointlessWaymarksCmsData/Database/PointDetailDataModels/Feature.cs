using PointlessWaymarksCmsData.Content;

namespace PointlessWaymarksCmsData.Database.PointDetailDataModels
{
    public class Feature : IPointDetailData
    {
        public string DataTypeIdentifier => "Feature";
        public string Notes { get; set; }
        public string NotesContentFormat { get; set; }
        public string Type { get; set; }

        public (bool isValid, string validationMessage) Validate()
        {
            return CommonContentValidation.ValidateTitle(Type);
        }
    }
}