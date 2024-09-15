using System.ComponentModel;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.PowerShellRunnerGui.Controls;

[NotifyPropertyChanged]
public partial class AppSettingsContext
{
    public AppSettingsContext()
    {
        PropertyChanged += AppSettingsContext_PropertyChanged;
    }

    public required string ProgramUpdateLocation { get; set; }
    public required PowerShellRunnerGuiSettings Settings { get; set; }
    public required StatusControlContext StatusContext { get; set; }


    private void AppSettingsContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (nameof(ProgramUpdateLocation).Equals(e.PropertyName))
        {
            Settings.ProgramUpdateDirectory = ProgramUpdateLocation;
#pragma warning disable CS4014
            //Allow call to continue without waiting and write settings
            PowerShellRunnerGuiSettingTools.WriteSettings(Settings);
#pragma warning restore CS4014
        }
    }

    public static async Task<AppSettingsContext> CreateInstance(StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factorySettings = PowerShellRunnerGuiSettingTools.ReadSettings();

        var factoryContext = new AppSettingsContext
        {
            StatusContext = statusContext,
            Settings = factorySettings,
            ProgramUpdateLocation = factorySettings.ProgramUpdateDirectory
        };

        return factoryContext;
    }
}