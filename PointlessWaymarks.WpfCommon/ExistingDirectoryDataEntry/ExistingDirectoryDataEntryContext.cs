using System.ComponentModel;
using System.IO;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.WpfCommon.ExistingDirectoryDataEntry;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class ExistingDirectoryDataEntryContext : IHasChanges, IHasValidationIssues
{
    private ExistingDirectoryDataEntryContext(StatusControlContext statusContext)
    {
        PropertyChanged += OnPropertyChanged;

        StatusContext = statusContext;

        BuildCommands();
    }

    public Func<string, Task> AfterDirectoryChoice { get; set; } = _ => Task.CompletedTask;
    public Func<Task<string>> GetInitialDirectory { get; set; } = () => Task.FromResult("");
    public string HelpText { get; set; } = string.Empty;
    public string ReferenceValue { get; set; } = string.Empty;
    public StatusControlContext StatusContext { get; set; }
    public string Title { get; set; } = string.Empty;
    public string UserValue { get; set; } = string.Empty;
    public List<Func<string?, Task<IsValid>>> ValidationFunctions { get; set; } = [];
    public string ValidationMessage { get; set; } = string.Empty;
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }


    public async Task CheckForChangesAndValidationIssues()
    {
        HasChanges = UserValue.TrimNullToEmpty() != ReferenceValue.TrimNullToEmpty();

        if (ValidationFunctions.Any())
            foreach (var loopValidations in ValidationFunctions)
            {
                var validationResult = await loopValidations(UserValue);
                if (!validationResult.Valid)
                {
                    HasValidationIssues = true;
                    ValidationMessage = validationResult.Explanation;
                    return;
                }
            }

        HasValidationIssues = false;
        ValidationMessage = string.Empty;
    }

    public static ExistingDirectoryDataEntryContext CreateInstance(StatusControlContext? statusContext)
    {
        return new ExistingDirectoryDataEntryContext(statusContext ?? new StatusControlContext());
    }

    private async void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName.Equals(nameof(ReferenceValue)) || e.PropertyName.Equals(nameof(UserValue)) ||
            e.PropertyName.Equals(nameof(ValidationFunctions)))
            await CheckForChangesAndValidationIssues();
    }

    [BlockingCommand]
    public async Task PickDirectory()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var initialDirectory = await GetInitialDirectory();

        var folderPicker = new VistaFolderBrowserDialog
            { Description = $"Choose Directory - {Title}", Multiselect = false };

        if (!string.IsNullOrWhiteSpace(initialDirectory) && Path.Exists(initialDirectory))
            folderPicker.SelectedPath = $"{initialDirectory}\\";

        if (folderPicker.ShowDialog() != true) return;

        UserValue = folderPicker.SelectedPath;

        await AfterDirectoryChoice(folderPicker.SelectedPath);
    }
}