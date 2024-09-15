using System.ComponentModel;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.FeedReaderGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class AppSettingsContext
{
    public AppSettingsContext(StatusControlContext context)
    {
        StatusContext = context;

        BuildCommands();

        Settings = FeedReaderGuiSettingTools.ReadSettings();

        ProgramUpdateLocation = Settings.ProgramUpdateDirectory;

        PropertyChanged += AppSettingsContext_PropertyChanged;
    }

    public string ProgramUpdateLocation { get; set; }
    public FeedReaderGuiSettings Settings { get; set; }

    public StatusControlContext StatusContext { get; set; }

    private void AppSettingsContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (nameof(ProgramUpdateLocation).Equals(e.PropertyName))
        {
            Settings.ProgramUpdateDirectory = ProgramUpdateLocation;
#pragma warning disable CS4014
            //Allow call to continue without waiting and write settings
            FeedReaderGuiSettingTools.WriteSettings(Settings);
#pragma warning restore CS4014
        }
    }

    [NonBlockingCommand]
    public async Task EnterBasicAuthDecryptionKey()
    {
        var settings = FeedReaderGuiSettingTools.ReadSettings();
        await FeedReaderEncryptionHelper.SetUserBasicAuthEncryptionKeyEntry(StatusContext, settings.LastDatabaseFile);
    }
}