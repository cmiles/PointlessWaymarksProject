using System.Windows;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.SearchBuilder;

/// <summary>
///     Interaction logic for LocationBoundsChooserWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class LocationBoundsChooserWindow
{
    public LocationBoundsChooserWindow()
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();
        DataContext = this;
    }

    public LocationBoundsChooserContext? LocationChooser { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; } = "Location Bounds Chooser";

    private async void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        DialogResult = false;
    }

    private async void ChooseLocationButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        DialogResult = true;
    }

    /// <summary>
    ///     Creates a new instance - this method can be called from any thread and will
    ///     switch to the UI thread as needed. Does not show the window - consider using
    ///     PositionWindowAndShowOnUiThread() from the WindowInitialPositionHelpers.
    /// </summary>
    /// <returns></returns>
    public static async Task<LocationBoundsChooserWindow> CreateInstance(SpatialBounds? initialBounds,
        string chooseForName)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new LocationBoundsChooserWindow
        {
            WindowTitle =
                $"Location Chooser{(string.IsNullOrEmpty(chooseForName) ? "" : $" - {chooseForName}")} - {UserSettingsSingleton.CurrentSettings().SiteName}"
        };

        await ThreadSwitcher.ResumeBackgroundAsync();

        window.LocationChooser = await LocationBoundsChooserContext.CreateInstance(window.StatusContext, initialBounds);
        await window.LocationChooser.LoadData();

        await ThreadSwitcher.ResumeForegroundAsync();

        return window;
    }
}