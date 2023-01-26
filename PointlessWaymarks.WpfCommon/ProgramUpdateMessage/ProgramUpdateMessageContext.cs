using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PointlessWaymarks.WpfCommon.ProgramUpdateMessage;

public partial class ProgramUpdateMessageContext : ObservableObject
{
    [ObservableProperty] private string _currentVersion;
    [ObservableProperty] private FileInfo _setupFile;
    [ObservableProperty] private bool _showMessage;
    [ObservableProperty] private string _updateMessage;
    [ObservableProperty] private string _updateVersion;

    public ProgramUpdateMessageContext()
    {
        UpdateCommand = new AsyncRelayCommand(Update);
        DismissCommand = new RelayCommand(Dismiss);
    }

    public RelayCommand DismissCommand { get; set; }

    public AsyncRelayCommand UpdateCommand { get; set; }

    public void Dismiss()
    {
        ShowMessage = false;
    }

    public async Task LoadData(string currentVersion, string updateVersion, FileInfo setupFile)
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
        Process.Start(SetupFile.FullName);
        Application.Current.Shutdown();
    }
}