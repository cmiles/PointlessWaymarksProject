#region

using System.IO;
using AnyClone;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Omu.ValueInjecter;
using PointlessWaymarks.GeoToolsGui.Models;
using PointlessWaymarks.GeoToolsGui.Settings;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

#endregion

namespace PointlessWaymarks.GeoToolsGui.Controls;

[ObservableObject]
public partial class FeatureFileEditorContext
{
    [ObservableProperty] private string _attributeToAdd = string.Empty;
    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private FeatureFileViewModel _model;
    [ObservableProperty] private FeatureFileViewModel _originalModelState;
    [ObservableProperty] private string _selectedAttribute = string.Empty;
    [ObservableProperty] private StatusControlContext _statusContext;

    public FeatureFileEditorContext(StatusControlContext? statusContext, FeatureFileViewModel? featureFile)
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _model = featureFile ?? new FeatureFileViewModel();
        _originalModelState = _model.Clone();

        CancelCommand = StatusContext.RunBlockingTaskCommand(Cancel);
        FinishEditCommand = StatusContext.RunBlockingTaskCommand(FinishEdit);
        AddAttributeCommand = StatusContext.RunNonBlockingTaskCommand(AddAttribute);
        RemoveAttributeCommand = StatusContext.RunNonBlockingTaskCommand<string>(RemoveAttribute);
    }

    public RelayCommand AddAttributeCommand { get; set; }

    public RelayCommand CancelCommand { get; set; }

    public EventHandler<FeatureFileEditorEndEditCondition>? EndEdit { get; set; }

    public RelayCommand FinishEditCommand { get; set; }

    public RelayCommand<string> RemoveAttributeCommand { get; set; }

    public async System.Threading.Tasks.Task AddAttribute()
    {
        if (string.IsNullOrEmpty(AttributeToAdd))
        {
            StatusContext.ToastWarning("Can't Add a Blank/Whitespace Only Attribute");
            return;
        }

        if (Model.AttributesForTags.Any(x => AttributeToAdd.Equals(x, StringComparison.OrdinalIgnoreCase)))
        {
            StatusContext.ToastWarning("Attribute Name already exists...");
            return;
        }

        await ThreadSwitcher.ResumeForegroundAsync();

        var newList = Model.AttributesForTags!;
        newList.Add(AttributeToAdd.Trim());
        newList = newList.OrderByDescending(x => x).ToList();

        Model.AttributesForTags = newList;
    }

    public async System.Threading.Tasks.Task Cancel()
    {
        Model.InjectFrom(OriginalModelState);
        EndEdit?.Invoke(this, FeatureFileEditorEndEditCondition.Cancelled);
        IsVisible = false;
    }

    public async System.Threading.Tasks.Task FinishEdit()
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

        var existingFeatures = (await FeatureIntersectTaggerSettingTools.ReadSettings()).FeatureIntersectFiles;

        if (!existingFeatures.Any())
        {
            await FeatureIntersectTaggerSettingTools.SetFeatureFiles(existingFeatures);
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

        await FeatureIntersectTaggerSettingTools.SetFeatureFiles(newFeatureFiles);

        EndEdit?.Invoke(this, FeatureFileEditorEndEditCondition.Saved);

        IsVisible = false;
    }

    public async System.Threading.Tasks.Task RemoveAttribute(string toRemove)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newList = Model.AttributesForTags!;
        newList.Remove(toRemove);
        newList = newList.OrderByDescending(x => x).ToList();

        Model.AttributesForTags = newList;
    }

    public void Show(FeatureFileViewModel model)
    {
        AttributeToAdd = string.Empty;
        Model = model;
        OriginalModelState = Model.Clone();
        SelectedAttribute = null;
        IsVisible = true;
    }
}