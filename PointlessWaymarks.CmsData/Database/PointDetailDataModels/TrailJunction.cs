using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.Database.PointDetailDataModels;

public class TrailJunction : IPointDetailData
{
    public string DataTypeIdentifier => "Trail Junction";
    public string? Notes { get; set; }
    public string NotesContentFormat { get; set; } = ContentFormatDefaults.Content.ToString();
    public bool? Sign { get; set; }

    public async Task<IsValid> Validate()
    {
        var formatValidation = await CommonContentValidation.ValidateBodyContentFormat(NotesContentFormat);
        return !formatValidation.Valid
            ? new IsValid(false, formatValidation.Explanation)
            : new IsValid(true, string.Empty);
    }
}