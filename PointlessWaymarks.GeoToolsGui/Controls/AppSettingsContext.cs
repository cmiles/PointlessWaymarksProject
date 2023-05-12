using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PointlessWaymarks.GeoToolsGui.Controls;

public partial class AppSettingsContext : ObservableObject
{
    [ObservableProperty] private GeoToolsGuiSettings _settings;
    [ObservableProperty] private string _programUpdateLocation;
    [ObservableProperty] private bool _showUpdateLocationExistsWarning;

    public AppSettingsContext()
    {
        _settings = GeoToolsGuiSettingTools.ReadSettings();

        _programUpdateLocation = Settings.ProgramUpdateDirectory;

        PropertyChanged += AppSettingsContext_PropertyChanged;

        ValidateProgramUpdateLocation();
    }

    private void AppSettingsContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (nameof(ProgramUpdateLocation).Equals(e.PropertyName))
        {
            ValidateProgramUpdateLocation();

            Settings.ProgramUpdateDirectory = ProgramUpdateLocation;
#pragma warning disable CS4014
            if (!ShowUpdateLocationExistsWarning) 
                //Allow call to continue without waiting and write settings
                GeoToolsGuiSettingTools.WriteSettings(Settings);
#pragma warning restore CS4014
        }
    }

    private void ValidateProgramUpdateLocation()
    {
        if (string.IsNullOrWhiteSpace(ProgramUpdateLocation)) ShowUpdateLocationExistsWarning = false;

        ShowUpdateLocationExistsWarning = !Directory.Exists(ProgramUpdateLocation);
    }
}