using PointlessWaymarks.CmsData.Content;

namespace PointlessWaymarks.CmsData.Database.PointDetailDataModels
{
    public class Feature : IPointDetailData
    {
        public string? Notes { get; set; }
        public string NotesContentFormat { get; set; } = ContentFormatDefaults.Content.ToString();
        public string? Type { get; set; }
        public string DataTypeIdentifier => "Feature";

        public IsValid Validate()
        {
            var formatValidation = CommonContentValidation.ValidateBodyContentFormat(NotesContentFormat);
            if (!formatValidation.Valid) return new IsValid(false, formatValidation.Explanation);

            return CommonContentValidation.ValidateFeatureType(Type);
        }
    }
}