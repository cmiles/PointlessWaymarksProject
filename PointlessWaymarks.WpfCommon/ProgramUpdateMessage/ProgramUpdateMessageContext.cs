using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PointlessWaymarks.WpfCommon.ProgramUpdateMessage;

public partial class ProgramUpdateMessageContext : ObservableObject
{
    [ObservableProperty] private string _currentVersion = string.Empty;
    [ObservableProperty] private FileInfo? _setupFile;
    [ObservableProperty] private bool _showMessage;
    [ObservableProperty] private string _updateMessage = string.Empty;
    [ObservableProperty] private string _updateVersion = string.Empty;

    public ProgramUpdateMessageContext()
    {
        UpdateCommand = new AsyncRelayCommand(Update);
        DismissCommand = new RelayCommand(Dismiss);
    }

    public RelayCommand DismissCommand { get; }

    public AsyncRelayCommand UpdateCommand { get; }

    public void Dismiss()
    {
        ShowMessage = false;
    }

#pragma warning disable CS1998
    //Async expected on this method by convention
    public async Task LoadData(string currentVersion, string updateVersion, FileInfo? setupFile)
#pragma warning restore CS1998
    {
        CurrentVersion = currentVersion;
        UpdateVersion = updateVersion;
        SetupFile = setupFile;

        if (string.IsNullOrEmpty(currentVersion) || string.IsNullOrWhiteSpace(UpdateVersion) ||
            SetupFile is not { Exists: true })
        {
            ShowMessage = false;
            return;
        }

        UpdateMessage =
            $"Update Available! Close Program and Update From {CurrentVersion} to {UpdateVersion} now? Make sure all work is saved first...";

        ShowMessage = true;
    }

    public async Task Update()
    {
        await ThreadSwitcher.ThreadSwitcher.ResumeForegroundAsync();
        
        Debug.Assert(SetupFile != null, nameof(SetupFile) + " != null");
        Process.Start(SetupFile.FullName);
        
        Application.Current.Shutdown();
    }
}