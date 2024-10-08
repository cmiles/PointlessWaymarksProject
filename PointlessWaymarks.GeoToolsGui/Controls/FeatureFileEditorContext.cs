using System.IO;
using AnyClone;
using Omu.ValueInjecter;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.GeoToolsGui.Models;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.GeoToolsGui.Controls;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class FeatureFileEditorContext
{
    private List<FeatureFileContext> _existingFeatureFileViewModels;

    public FeatureFileEditorContext(StatusControlContext statusContext, FeatureFileContext? featureFile,
        List<FeatureFileContext> existingFeatureFileViewModels)
    {
        StatusContext = statusContext;
        Model = featureFile ?? new FeatureFileContext();

        BuildCommands();

        OriginalModelState = Model.Clone();
        _existingFeatureFileViewModels = existingFeatureFileViewModels;
    }

    public string AttributeToAdd { get; set; } = string.Empty;

    public EventHandler<(FeatureFileEditorEndEditCondition endCondition, FeatureFileContext model)>? EndEdit
    {
        get;
        set;
    }

    public bool IsVisible { get; set; }
    public FeatureFileContext Model { get; set; }
    public FeatureFileContext OriginalModelState { get; set; }
    public string? SelectedAttribute { get; set; } = string.Empty;
    public StatusControlContext StatusContext { get; }

    [NonBlockingCommand]
    public async Task AddAttribute()
    {
        if (string.IsNullOrEmpty(AttributeToAdd))
        {
            await StatusContext.ToastWarning("Can't Add a Blank/Whitespace Only Attribute");
            return;
        }

        if (Model.AttributesForTags.Any(x => AttributeToAdd.Equals(x, StringComparison.OrdinalIgnoreCase)))
        {
            await StatusContext.ToastWarning("Attribute Name already exists...");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var newList = Model.AttributesForTags;
        newList.Add(AttributeToAdd.Trim());
        newList = newList.OrderByDescending(x => x).ToList();

        Model.AttributesForTags = newList;
    }

    [BlockingCommand]
    public Task Cancel()
    {
        Model.InjectFrom(OriginalModelState);
        EndEdit?.Invoke(this, (FeatureFileEditorEndEditCondition.Cancelled, Model));
        IsVisible = false;
        return Task.CompletedTask;
    }

    [BlockingCommand]
    public async Task ChooseFile()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var filePicker = new VistaOpenFileDialog { Filter = "geojson files (*.geojson)|*.geojson|All files (*.*)|*.*" };

        if (!string.IsNullOrWhiteSpace(Model.FileName))
        {
            var currentFileInfo = new FileInfo(Model.FileName);
            if (currentFileInfo.Exists && !string.IsNullOrWhiteSpace(currentFileInfo.Directory?.FullName))
                filePicker.FileName = $"{currentFileInfo.Directory.FullName}\\";
        }

        var result = filePicker.ShowDialog();

        if (!result ?? false) return;

        await ThreadSwitcher.ResumeBackgroundAsync();

        var possibleFile = new FileInfo(filePicker.FileName);

        if (!possibleFile.Exists)
        {
            await StatusContext.ToastError("File doesn't exist?");
            return;
        }

        Model.FileName = possibleFile.FullName;
    }

    [BlockingCommand]
    public async Task FinishEdit()
    {
        if (string.IsNullOrEmpty(Model.FileName))
        {
            await StatusContext.ToastWarning("Can not add a Feature File with a Blank Filename");
            return;
        }

        var fileInfo = new FileInfo(Model.FileName);

        if (!fileInfo.Exists)
        {
            await StatusContext.ToastWarning($"{Model.FileName} does not exist?");
            return;
        }

        if (string.IsNullOrWhiteSpace(Model.Name))
        {
            await StatusContext.ToastWarning($"Please provide a name for {Model.FileName}");
            return;
        }

        if (string.IsNullOrWhiteSpace(Model.TagAll) && !Model.AttributesForTags.Any())
        {
            await StatusContext.ToastWarning("Tag All With or at least on Attribute for Tags must be set");
            return;
        }

        var possibleExisting = _existingFeatureFileViewModels.Where(x =>
            x.ContentId != Model.ContentId && x.FileName.Equals(Model.FileName, StringComparison.OrdinalIgnoreCase) &&
            x.Name.Equals(Model.Name, StringComparison.OrdinalIgnoreCase)).ToList();

        if (possibleExisting.Any())
        {
            await StatusContext.ToastWarning("The File Name and Name must be unique...");
            return;
        }

        EndEdit?.Invoke(this, (FeatureFileEditorEndEditCondition.Saved, Model));

        IsVisible = false;
        return;
    }

    [NonBlockingCommand]
    public async Task RemoveAttribute(string? toRemove)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newList = Model.AttributesForTags;
        newList.Remove(toRemove ?? string.Empty);
        newList = newList.OrderByDescending(x => x).ToList();

        Model.AttributesForTags = newList;
    }

    public void Show(FeatureFileContext model, List<FeatureFileContext> existingFeatureFileViewModels)
    {
        AttributeToAdd = string.Empty;
        Model = model;
        _existingFeatureFileViewModels = existingFeatureFileViewModels;
        OriginalModelState = Model.Clone();
        SelectedAttribute = null;
        IsVisible = true;
    }
}