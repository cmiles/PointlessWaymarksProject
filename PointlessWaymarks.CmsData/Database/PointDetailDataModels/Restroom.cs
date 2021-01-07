using PointlessWaymarks.CmsData.Content;

namespace PointlessWaymarks.CmsData.Database.PointDetailDataModels
{
    public class Restroom : IPointDetailData
    {
        public string DataTypeIdentifier => "Restroom";
        public string Notes { get; set; }
        public string NotesContentFormat { get; set; } = ContentFormatDefaults.Content.ToString();

        public (bool isValid, string validationMessage) Validate()
        {
            var formatValidation = CommonContentValidation.ValidateBodyContentFormat(NotesContentFormat);
            if (!formatValidation.isValid) return (false, formatValidation.explanation);

            return (true, string.Empty);
        }
    }
}