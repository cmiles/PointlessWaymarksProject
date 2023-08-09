using System.ComponentModel;
using System.IO;
using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.FeedReaderGui.Controls;

[NotifyPropertyChanged]
public partial class AppSettingsContext
{
    public AppSettingsContext()
    {
        Settings = FeedReaderGuiSettingTools.ReadSettings();

        ProgramUpdateLocation = Settings.ProgramUpdateDirectory;

        PropertyChanged += AppSettingsContext_PropertyChanged;

        ValidateProgramUpdateLocation();
    }

    public string ProgramUpdateLocation { get; set; }
    public FeedReaderGuiSettings Settings { get; set; }
    public bool ShowUpdateLocationExistsWarning { get; set; }

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
                FeedReaderGuiSettingTools.WriteSettings(Settings);
#pragma warning restore CS4014
        }
    }

    private void ValidateProgramUpdateLocation()
    {
        if (string.IsNullOrWhiteSpace(ProgramUpdateLocation)) ShowUpdateLocationExistsWarning = false;

        ShowUpdateLocationExistsWarning = !Directory.Exists(ProgramUpdateLocation);
    }
}