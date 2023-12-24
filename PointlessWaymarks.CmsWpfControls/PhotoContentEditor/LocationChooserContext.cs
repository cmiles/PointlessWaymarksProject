using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.PhotoContentEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class LocationChooserContext
{
    public bool BroadcastLatLongChange { get; set; } = true;
    public ConversionDataEntryContext<double?>? ElevationEntry { get; set; }
    public double? InitialElevation { get; set; }

    public double InitialLatitude { get; set; }
    public double InitialLongitude { get; set; }
    public ConversionDataEntryContext<double>? LatitudeEntry { get; set; }
    public ConversionDataEntryContext<double>? LongitudeEntry { get; set; }
    public StatusControlContext StatusContext { get; set; }

    [BlockingCommand]
    public async Task GetElevation()
    {
        if (LatitudeEntry!.HasValidationIssues || LongitudeEntry!.HasValidationIssues)
        {
            StatusContext.ToastError("Lat Long is not valid");
            return;
        }

        var possibleElevation =
            await ElevationGuiHelper.GetElevation(LatitudeEntry.UserValue, LongitudeEntry.UserValue, StatusContext);

        if (possibleElevation != null) ElevationEntry!.UserText = possibleElevation.Value.ToString("F2");
    }

    private void LatitudeLongitudeChangeBroadcast()
    {
        if (BroadcastLatLongChange && !LatitudeEntry!.HasValidationIssues && !LongitudeEntry!.HasValidationIssues)
            RaisePointLatitudeLongitudeChange?.Invoke(this,
                new PointLatitudeLongitudeChange(LatitudeEntry.UserValue, LongitudeEntry.UserValue));
    }

    public async Task LoadData()
    {
        ElevationEntry =
            await ConversionDataEntryContext<double?>.CreateInstance(
                ConversionDataEntryHelpers.DoubleNullableConversion);
        ElevationEntry.ValidationFunctions = new List<Func<double?, Task<IsValid>>>
        {
            CommonContentValidation.ElevationValidation
        };
        ElevationEntry.ComparisonFunction = (o, u) => (o == null && u == null) || o.IsApproximatelyEqualTo(u, .001);
        ElevationEntry.Title = "Elevation";
        ElevationEntry.HelpText = "Elevation in Feet";
        ElevationEntry.ReferenceValue = InitialElevation;
        ElevationEntry.UserText = InitialElevation?.ToString("F2") ?? string.Empty;

        LatitudeEntry =
            await ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        LatitudeEntry.ValidationFunctions = new List<Func<double, Task<IsValid>>>
        {
            CommonContentValidation.LatitudeValidation
        };
        LatitudeEntry.ComparisonFunction = (o, u) => o.IsApproximatelyEqualTo(u, .000001);
        LatitudeEntry.Title = "Latitude";
        LatitudeEntry.HelpText = "In DDD.DDDDDD°";
        LatitudeEntry.ReferenceValue = InitialLatitude;
        LatitudeEntry.UserText = InitialLatitude.ToString("F6");
        LatitudeEntry.PropertyChanged += (_, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.PropertyName)) return;
            if (args.PropertyName == nameof(LatitudeEntry.UserValue)) LatitudeLongitudeChangeBroadcast();
        };

        LongitudeEntry =
            await ConversionDataEntryContext<double>.CreateInstance(ConversionDataEntryHelpers.DoubleConversion);
        LongitudeEntry.ValidationFunctions = new List<Func<double, Task<IsValid>>>
        {
            CommonContentValidation.LongitudeValidation
        };
        LongitudeEntry.ComparisonFunction = (o, u) => o.IsApproximatelyEqualTo(u, .000001);
        LongitudeEntry.Title = "Longitude";
        LongitudeEntry.HelpText = "In DDD.DDDDDD°";
        LongitudeEntry.ReferenceValue = InitialLongitude;
        LongitudeEntry.UserText = InitialLongitude.ToString("F6");
        LongitudeEntry.PropertyChanged += (_, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.PropertyName)) return;
            if (args.PropertyName == nameof(LongitudeEntry.UserValue)) LatitudeLongitudeChangeBroadcast();
        };
    }

    public void OnRaisePointLatitudeLongitudeChange(object? sender, PointLatitudeLongitudeChange e)
    {
        BroadcastLatLongChange = false;

        LatitudeEntry!.UserText = e.Latitude.ToString("F6");
        LongitudeEntry!.UserText = e.Longitude.ToString("F6");

        BroadcastLatLongChange = true;
    }

    public event EventHandler<PointLatitudeLongitudeChange>? RaisePointLatitudeLongitudeChange;
}