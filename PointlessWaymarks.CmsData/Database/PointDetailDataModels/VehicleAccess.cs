using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.CmsData.Database.PointDetailDataModels;

public class VehicleAccess : IPointDetailData
{
    public string DataTypeIdentifier => "Vehicle Access";
    public bool RecommendedForPassengerCar { get; set; }
    public bool RecommendedTwoWheelDriveModerateClearance { get; set; }
    public bool RecommendedFourWheelDriveHighClearance { get; set; }
    public string? Notes { get; set; }
    public string? NotesContentFormat { get; set; } = ContentFormatDefaults.Content.ToString();

    public async Task<IsValid> Validate()
    {
        var formatValidation = await CommonContentValidation.ValidateBodyContentFormat(NotesContentFormat);
        return !formatValidation.Valid
            ? new IsValid(false, formatValidation.Explanation)
            : new IsValid(true, string.Empty);
    }
}