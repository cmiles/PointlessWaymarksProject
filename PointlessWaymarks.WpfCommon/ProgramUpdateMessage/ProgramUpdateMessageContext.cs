using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using Serilog;

namespace PointlessWaymarks.WpfCommon.ProgramUpdateMessage;

[NotifyPropertyChanged]
public partial class ProgramUpdateMessageContext
{
    public ProgramUpdateMessageContext()
    {
        UpdateCommand = new AsyncRelayCommand(Update);
        DismissCommand = new RelayCommand(Dismiss);
    }

    public string CurrentVersion { get; set; } = string.Empty;
    public RelayCommand DismissCommand { get; }
    public FileInfo? SetupFile { get; set; }
    public bool ShowMessage { get; set; }
    public AsyncRelayCommand UpdateCommand { get; }
    public string UpdateMessage { get; set; } = string.Empty;
    public string UpdateVersion { get; set; } = string.Empty;

    public void Dismiss()
    {
        ShowMessage = false;
    }

    //Async expected on this method by convention
    public Task LoadData(string currentVersion, string updateVersion, FileInfo? setupFile)
    {
        CurrentVersion = currentVersion;
        UpdateVersion = updateVersion;
        SetupFile = setupFile;

        if (string.IsNullOrEmpty(currentVersion) || string.IsNullOrWhiteSpace(UpdateVersion) ||
            SetupFile is not { Exists: true })
        {
            ShowMessage = false;
            return Task.CompletedTask;
        }

        UpdateMessage =
            $"Update Available! Close Program and Update From {CurrentVersion} to {UpdateVersion} now? Make sure all work is saved first...";

        ShowMessage = true;

        Log.ForContext(nameof(ProgramUpdateMessageContext), this.SafeObjectDump())
            .Information("Program Update Message Context Loaded - Show Update Message {showUpdate}", ShowMessage);

        return Task.CompletedTask;
    }

    public async Task Update()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        Debug.Assert(SetupFile != null, nameof(SetupFile) + " != null");
        Process.Start(SetupFile.FullName);

        Application.Current.Shutdown();
    }
}