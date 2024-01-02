using System.Windows;
using Microsoft.Web.WebView2.Core;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.PointContentEditor;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.PhotoContentEditor;

[NotifyPropertyChanged]
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
    public string WindowTitle { get; set; }

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
            WindowTitle = $"Location Chooser{(string.IsNullOrEmpty(chooseForName) ? "" : $" - {chooseForName}")} - {UserSettingsSingleton.CurrentSettings().SiteName}"
        };

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.LocationChooser = await LocationChooserContext.CreateInstance(window.StatusContext, initialLatitude, initialLongitude,  initialElevation);
        await window.LocationChooser.LoadData();
        
        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }

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
            StatusContext.ToastError("Validation Error...");
            return;
        }
        
        DialogResult = true;
    }
}