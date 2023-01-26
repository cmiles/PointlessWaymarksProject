using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.GeoToolsGui.Controls;

public partial class AppSettingsContext : ObservableObject
{
    [ObservableProperty] private string _programUpdateLocation;
    [ObservableProperty] private bool _showUpdateLocationExistsWarning;

    public AppSettingsContext()
    {
        _programUpdateLocation = GeoToolsGuiAppSettings.Default.ProgramUpdateLocation;

        PropertyChanged += AppSettingsContext_PropertyChanged;

        ValidateProgramUpdateLocation();
    }

    private void AppSettingsContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (nameof(ProgramUpdateLocation).Equals(e.PropertyName))
        {
            GeoToolsGuiAppSettings.Default.ProgramUpdateLocation = e.PropertyName;
            GeoToolsGuiAppSettings.Default.Save();

            ValidateProgramUpdateLocation();
        }
    }

    private void ValidateProgramUpdateLocation()
    {
        if (string.IsNullOrWhiteSpace(ProgramUpdateLocation)) ShowUpdateLocationExistsWarning = false;

        ShowUpdateLocationExistsWarning = !Directory.Exists(ProgramUpdateLocation);
    }
}