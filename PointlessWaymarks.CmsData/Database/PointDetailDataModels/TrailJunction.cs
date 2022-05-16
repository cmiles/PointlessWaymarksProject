using PointlessWaymarks.CmsData.Content;

namespace PointlessWaymarks.CmsData.Database.PointDetailDataModels;

public class TrailJunction : IPointDetailData
{
    public string? Notes { get; set; }
    public string NotesContentFormat { get; set; } = ContentFormatDefaults.Content.ToString();
    public bool? Sign { get; set; }
    public string DataTypeIdentifier => "Trail Junction";

    public async Task<IsValid> Validate()
    {
        var formatValidation = await CommonContentValidation.ValidateBodyContentFormat(NotesContentFormat);
        if (!formatValidation.Valid) return new IsValid(false, formatValidation.Explanation);

        return new IsValid(true, string.Empty);
    }
}