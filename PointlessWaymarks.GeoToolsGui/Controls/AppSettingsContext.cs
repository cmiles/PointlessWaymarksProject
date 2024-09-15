using System.ComponentModel;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.GeoToolsGui.Controls;

[NotifyPropertyChanged]
public partial class AppSettingsContext
{
    public AppSettingsContext()
    {
        Settings = GeoToolsGuiSettingTools.ReadSettings();

        ProgramUpdateLocation = Settings.ProgramUpdateDirectory;

        PropertyChanged += AppSettingsContext_PropertyChanged;
    }

    public string ProgramUpdateLocation { get; set; }
    public GeoToolsGuiSettings Settings { get; set; }

    private void AppSettingsContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (nameof(ProgramUpdateLocation).Equals(e.PropertyName))
        {
            Settings.ProgramUpdateDirectory = ProgramUpdateLocation;
#pragma warning disable CS4014
            //Allow call to continue without waiting and write settings
            GeoToolsGuiSettingTools.WriteSettings(Settings);
#pragma warning restore CS4014
        }
    }
}