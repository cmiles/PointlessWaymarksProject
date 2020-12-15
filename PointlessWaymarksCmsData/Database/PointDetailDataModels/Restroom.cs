using PointlessWaymarksCmsData.Content;

namespace PointlessWaymarksCmsData.Database.PointDetailDataModels
{
    public class Restroom : IPointDetailData
    {
        public string Notes { get; set; }
        public string NotesContentFormat { get; set; } = ContentFormatDefaults.Content.ToString();
        public string DataTypeIdentifier => "Restroom";

        public (bool isValid, string validationMessage) Validate()
        {
            var formatValidation = CommonContentValidation.ValidateBodyContentFormat(NotesContentFormat);
            if (!formatValidation.isValid) return (false, formatValidation.explanation);

            return (true, string.Empty);
        }
    }
}