using System.ComponentModel;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsData.Spatial;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.FeatureIntersectionTags;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.BoolDataEntry;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.ConversionDataEntry;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.CmsWpfControls.OptionalLocationEntry;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class OptionalLocationEntryContext : IHasChanges, IHasValidationIssues
{
    public OptionalLocationEntryContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext;
        
        BuildCommands();
        
        PropertyChanged += OnPropertyChanged;
    }
    
    public ConversionDataEntryContext<double?>? ElevationEntry { get; set; }
    public ConversionDataEntryContext<double?>? LatitudeEntry { get; set; }
    public ConversionDataEntryContext<double?>? LongitudeEntry { get; set; }
    public BoolDataEntryContext? ShowLocationEntry { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }
    
    
    public void CheckForChangesAndValidationIssues()
    {
        HasChanges = PropertyScanners.ChildPropertiesHaveChanges(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this);
    }
    
    public static async Task<OptionalLocationEntryContext> CreateInstance(StatusControlContext statusContext,
        IOptionalLocation optionalLocationContent)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var newContext = new OptionalLocationEntryContext(statusContext);
        await newContext.LoadData(optionalLocationContent);
        
        return newContext;
    }
    
    /// <summary>
    ///     Returns a NTS Feature based on the current Lat/Long - if values are null or invalid
    ///     null is returned.
    /// </summary>
    /// <returns></returns>
    public async Task<IFeature?> FeatureFromPoint()
    {
        if (LatitudeEntry!.UserValue == null || LongitudeEntry!.UserValue == null) return null;
        
        var latitudeValidation =
            await CommonContentValidation.LatitudeValidation(LatitudeEntry.UserValue.Value);
        var longitudeValidation =
            await CommonContentValidation.LongitudeValidation(LongitudeEntry.UserValue.Value);
        
        if (!latitudeValidation.Valid || !longitudeValidation.Valid) return null;
        
        if (ElevationEntry!.UserValue is null)
            return new Feature(
                new Point(LongitudeEntry.UserValue.Value,
                    LatitudeEntry.UserValue.Value),
                new AttributesTable());
        return new Feature(
            new Point(LongitudeEntry.UserValue.Value,
                LatitudeEntry.UserValue.Value,
                ElevationEntry.UserValue.Value),
            new AttributesTable());
    }
    
    [BlockingCommand]
    public async Task GetElevation()
    {
        if (LatitudeEntry!.HasValidationIssues || LongitudeEntry!.HasValidationIssues)
        {
            StatusContext.ToastError("Lat Long is not valid");
            return;
        }
        
        if (LatitudeEntry.UserValue == null || LongitudeEntry.UserValue == null)
        {
            StatusContext.ToastError("Lat Long is not set");
            return;
        }
        
        var possibleElevation = await ElevationGuiHelper.GetElevation(LatitudeEntry.UserValue.Value,
            LongitudeEntry.UserValue.Value, StatusContext);
        
        if (possibleElevation != null) ElevationEntry!.UserText = possibleElevation.Value.MetersToFeet().ToString("N0");
    }
    
    public async Task<List<string>> GetFeatureIntersectTagsWithUiAlerts()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        var featureToCheck = await FeatureFromPoint();
        if (featureToCheck == null)
        {
            StatusContext.ToastError("No valid Lat/Long to check?");
            return new List<string>();
        }
        
        if (string.IsNullOrWhiteSpace(UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile))
        {
            StatusContext.ToastError(
                "To use this feature the Feature Intersect Settings file must be set in the Site Settings...");
            return new List<string>();
        }
        
        var possibleTags = featureToCheck.IntersectionTags(
            UserSettingsSingleton.CurrentSettings().FeatureIntersectionTagSettingsFile,
            CancellationToken.None, StatusContext.ProgressTracker());
        
        if (!possibleTags.Any()) StatusContext.ToastWarning("No tags found...");
        
        return new List<string>();
    }
    
    [BlockingCommand]
    public async Task GetLocationOnMap()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var window = await LocationChooserWindow.CreateInstance(LatitudeEntry!.UserValue, LongitudeEntry!.UserValue,
            ElevationEntry!.UserValue, "Pick Location");
        
        var result = await window.PositionWindowAndShowDialogOnUiThread();
        
        if (!result ?? true) return;
        
        LatitudeEntry.UserText = window.LocationChooser.LatitudeEntry.UserText;
        LongitudeEntry.UserText = window.LocationChooser.LongitudeEntry.UserText;
        ElevationEntry.UserText = window.LocationChooser.ElevationEntry.UserText;
    }
    
    public async Task LoadData(IOptionalLocation optionalLocationContent)
    {
        ShowLocationEntry = await BoolDataEntryContext.CreateInstance();
        ShowLocationEntry.Title = "Show Location";
        ShowLocationEntry.HelpText =
            "If enabled the web page built from this content will show, and have links to the location";
        ShowLocationEntry.ReferenceValue = optionalLocationContent.ShowLocation;
        ShowLocationEntry.UserValue = optionalLocationContent.ShowLocation;
        
        LatitudeEntry =
            await ConversionDataEntryContext<double?>.CreateInstance(
                ConversionDataEntryHelpers.DoubleNullableConversion);
        LatitudeEntry.ValidationFunctions = [CommonContentValidation.LatitudeValidationWithNullOk];
        LatitudeEntry.ComparisonFunction = (o, u) => o.IsApproximatelyEqualTo(u, .0000001);
        LatitudeEntry.Title = "Latitude";
        LatitudeEntry.HelpText = "In DDD.DDDDDD°";
        LatitudeEntry.ReferenceValue = optionalLocationContent.Latitude;
        LatitudeEntry.UserText = optionalLocationContent.Latitude?.ToString("F6") ?? string.Empty;
        
        LongitudeEntry =
            await ConversionDataEntryContext<double?>.CreateInstance(
                ConversionDataEntryHelpers.DoubleNullableConversion);
        LongitudeEntry.ValidationFunctions = [CommonContentValidation.LongitudeValidationWithNullOk];
        LongitudeEntry.ComparisonFunction = (o, u) => o.IsApproximatelyEqualTo(u, .0000001);
        LongitudeEntry.Title = "Longitude";
        LongitudeEntry.HelpText = "In DDD.DDDDDD°";
        LongitudeEntry.ReferenceValue = optionalLocationContent.Longitude;
        LongitudeEntry.UserText = optionalLocationContent.Longitude?.ToString("F6") ?? string.Empty;
        
        ElevationEntry =
            await ConversionDataEntryContext<double?>.CreateInstance(
                ConversionDataEntryHelpers.DoubleNullableConversion);
        ElevationEntry.ValidationFunctions = [CommonContentValidation.ElevationValidation];
        ElevationEntry.ComparisonFunction = (o, u) => o.IsApproximatelyEqualTo(u, .001);
        ElevationEntry.Title = "Elevation (feet)";
        ElevationEntry.HelpText = "Elevation in Feet";
        ElevationEntry.ReferenceValue = optionalLocationContent.Elevation;
        ElevationEntry.UserText = optionalLocationContent.Elevation?.ToString("N0") ?? string.Empty;
        
        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }
    
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;
        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }
}