using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.FeatureIntersectionTaggingGui.Models;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.FeatureIntersectionTaggingGui;

[ObservableObject]
public partial class FeatureFileEditorViewModel
{
    [ObservableProperty] private string _attributeToAdd = string.Empty;
    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private FeatureFileViewModel _model;
    [ObservableProperty] private string _selectedAttribute = string.Empty;
    [ObservableProperty] private StatusControlContext _statusContext;

    public FeatureFileEditorViewModel(StatusControlContext? statusContext, FeatureFileViewModel? featureFile)
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _model = featureFile ?? new FeatureFileViewModel();

        CancelCommand = StatusContext.RunBlockingTaskCommand(Cancel);
        FinishEditCommand = StatusContext.RunBlockingTaskCommand(FinishEdit);
        AddAttributeCommand = StatusContext.RunNonBlockingTaskCommand(AddAttribute);
    }

    public RelayCommand AddAttributeCommand { get; set; }

    public RelayCommand CancelCommand { get; set; }

    public EventHandler<FeatureFileEditorEndEditCondition>? EndEdit { get; set; }

    public RelayCommand FinishEditCommand { get; set; }

    public void Show(FeatureFileViewModel model)
    {
        AttributeToAdd = string.Empty;
        Model = model;
        SelectedAttribute = null;
        IsVisible = true;
    }

    public async Task AddAttribute()
    {
        if (string.IsNullOrEmpty(AttributeToAdd))
        {
            StatusContext.ToastWarning("Can't Add a Blank/Whitespace Only Attribute");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var newList = Model.AttributesForTags!;
        newList.Add(AttributeToAdd.Trim());
        newList = newList.OrderByDescending(x => x).ToList();

        Model.AttributesForTags = newList;
    }

    public async Task Cancel()
    {
        EndEdit?.Invoke(this, FeatureFileEditorEndEditCondition.Cancelled);
        IsVisible = false;
    }

    public async Task FinishEdit()
    {
        if (string.IsNullOrEmpty(Model.FileName))
        {
            StatusContext.ToastWarning("Can not add a Feature File with a Blank Filename");
            return;
        }

        var fileInfo = new FileInfo(Model.FileName);

        if (!fileInfo.Exists)
        {
            StatusContext.ToastWarning($"{Model.FileName} does not exist?");
            return;
        }

        if (string.IsNullOrWhiteSpace(Model.Name))
        {
            StatusContext.ToastWarning($"Please provide a name for {Model.FileName}");
            return;
        }

        if (string.IsNullOrWhiteSpace(Model.TagAll) && !Model.AttributesForTags.Any())
        {
            StatusContext.ToastWarning("Tag All With or at least on Attribute for Tags must be set");
            return;
        }

        var existingFeatures = (await FeatureIntersectionGuiSettingTools.ReadSettings()).FeatureIntersectFiles;

        if (!existingFeatures.Any())
        {
            await FeatureIntersectionGuiSettingTools.SetFeatureFiles(existingFeatures);
            EndEdit?.Invoke(this, FeatureFileEditorEndEditCondition.Saved);
            IsVisible = false;
        }

        var possibleExisting = existingFeatures.Where(x =>
            x.ContentId != Model.ContentId && x.FileName.Equals(Model.FileName, StringComparison.OrdinalIgnoreCase) &&
            x.Name.Equals(Model.Name, StringComparison.OrdinalIgnoreCase)).ToList();

        if (possibleExisting.Any())
        {
            StatusContext.ToastWarning("The File Name and Name must be unique...");
            return;
        }

        var newFeatureFiles = existingFeatures.Where(x => x.ContentId != Model.ContentId).ToList();
        newFeatureFiles.Add(Model);

        await FeatureIntersectionGuiSettingTools.SetFeatureFiles(newFeatureFiles);

        EndEdit?.Invoke(this, FeatureFileEditorEndEditCondition.Saved);

        IsVisible = false;
    }
}