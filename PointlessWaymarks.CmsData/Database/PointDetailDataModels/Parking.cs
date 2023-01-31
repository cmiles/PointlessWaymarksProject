using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.Database.PointDetailDataModels;

public class Parking : IPointDetailData
{
    public bool Fee { get; set; }
    public string? Notes { get; set; }
    public string? NotesContentFormat { get; set; } = ContentFormatDefaults.Content.ToString();
    public string DataTypeIdentifier => "Parking";

    public async Task<IsValid> Validate()
    {
        var formatValidation = await CommonContentValidation.ValidateBodyContentFormat(NotesContentFormat);
        if (!formatValidation.Valid) return new IsValid(false, formatValidation.Explanation);

        return new IsValid(true, string.Empty);
    }
}