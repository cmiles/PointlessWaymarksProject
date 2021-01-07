using PointlessWaymarks.CmsData.Content;

namespace PointlessWaymarks.CmsData.Database.PointDetailDataModels
{
    public class TrailJunction : IPointDetailData
    {
        public string DataTypeIdentifier => "Trail Junction";
        public string Notes { get; set; }
        public string NotesContentFormat { get; set; } = ContentFormatDefaults.Content.ToString();
        public bool? Sign { get; set; }

        public (bool isValid, string validationMessage) Validate()
        {
            var formatValidation = CommonContentValidation.ValidateBodyContentFormat(NotesContentFormat);
            if (!formatValidation.isValid) return (false, formatValidation.explanation);

            return (true, string.Empty);
        }
    }
}