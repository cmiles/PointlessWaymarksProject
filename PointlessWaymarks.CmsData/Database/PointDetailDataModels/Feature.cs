using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.Database.PointDetailDataModels;

public class Feature : IPointDetailData
{
    public string DataTypeIdentifier => "Feature";
    public string? Notes { get; set; }
    public string NotesContentFormat { get; set; } = ContentFormatDefaults.Content.ToString();
    public string? Type { get; set; }

    public async Task<IsValid> Validate()
    {
        var formatValidation = await CommonContentValidation.ValidateBodyContentFormat(NotesContentFormat);
        return !formatValidation.Valid
            ? new IsValid(false, formatValidation.Explanation)
            : await CommonContentValidation.ValidateFeatureType(Type);
    }
}