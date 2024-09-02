using System.Windows;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.OptionalLocationEntry;

[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class LocationChooserWindow
{
    public LocationChooserWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }
    
    public LocationChooserContext? LocationChooser { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; } = "Location Chooser";
    
    private async void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        
        DialogResult = false;
    }
    
    private async void ChooseLocationButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        
        if (LocationChooser.HasValidationIssues)
        {
            await StatusContext.ToastError("Validation Error...");
            return;
        }
        
        DialogResult = true;
    }
    
    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<LocationChooserWindow> CreateInstance(double? initialLatitude, double? initialLongitude,
        double? initialElevation, string chooseForName)
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        
        var window = new LocationChooserWindow
        {
            WindowTitle =
                $"Location Chooser{(string.IsNullOrEmpty(chooseForName) ? "" : $" - {chooseForName}")} - {UserSettingsSingleton.CurrentSettings().SiteName}"
        };
        
        await ThreadSwitcher.ResumeBackgroundAsync();
        
        window.LocationChooser = await LocationChooserContext.CreateInstance(window.StatusContext, initialLatitude,
            initialLongitude, initialElevation);
        await window.LocationChooser.LoadData();
        
        await ThreadSwitcher.ResumeForegroundAsync();
        
        return window;
    }
}