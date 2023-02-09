using System.IO;
using AnyClone;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.GeoToolsGui.Models;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.GeoToolsGui.Controls;

public partial class FeatureFileEditorContext : ObservableObject
{
    [ObservableProperty] private string _attributeToAdd = string.Empty;
    private List<FeatureFileViewModel> _existingFeatureFileViewModels;
    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private readonly FeatureFileViewModel _model;
    [ObservableProperty] private FeatureFileViewModel _originalModelState;
    [ObservableProperty] private string? _selectedAttribute = string.Empty;
    [ObservableProperty] private StatusControlContext _statusContext;

    public FeatureFileEditorContext(StatusControlContext? statusContext, FeatureFileViewModel? featureFile,
        List<FeatureFileViewModel> existingFeatureFileViewModels)
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _model = featureFile ?? new FeatureFileViewModel();
        _originalModelState = _model.Clone();
        _existingFeatureFileViewModels = existingFeatureFileViewModels;

        CancelCommand = StatusContext.RunBlockingTaskCommand(Cancel);
        FinishEditCommand = StatusContext.RunBlockingTaskCommand(FinishEdit);
        AddAttributeCommand = StatusContext.RunNonBlockingTaskCommand(AddAttribute);
        RemoveAttributeCommand = StatusContext.RunNonBlockingTaskCommand<string>(RemoveAttribute);
    }

    public RelayCommand AddAttributeCommand { get; set; }

    public RelayCommand CancelCommand { get; set; }

    public EventHandler<(FeatureFileEditorEndEditCondition endCondition, FeatureFileViewModel model)>? EndEdit
    {
        get;
        set;
    }

    public RelayCommand FinishEditCommand { get; set; }

    public RelayCommand<string> RemoveAttributeCommand { get; set; }

    public async Task AddAttribute()
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

    public async Task Cancel()
    {
        Model.InjectFrom(OriginalModelState);
        EndEdit?.Invoke(this, (FeatureFileEditorEndEditCondition.Cancelled, Model));
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

        var possibleExisting = _existingFeatureFileViewModels.Where(x =>
            x.ContentId != Model.ContentId && x.FileName.Equals(Model.FileName, StringComparison.OrdinalIgnoreCase) &&
            x.Name.Equals(Model.Name, StringComparison.OrdinalIgnoreCase)).ToList();

        if (possibleExisting.Any())
        {
            StatusContext.ToastWarning("The File Name and Name must be unique...");
            return;
        }

        EndEdit?.Invoke(this, (FeatureFileEditorEndEditCondition.Saved, Model));

        IsVisible = false;
    }

    public async Task RemoveAttribute(string toRemove)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var newList = Model.AttributesForTags!;
        newList.Remove(toRemove);
        newList = newList.OrderByDescending(x => x).ToList();

        Model.AttributesForTags = newList;
    }

    public void Show(FeatureFileViewModel model, List<FeatureFileViewModel> existingFeatureFileViewModels)
    {
        AttributeToAdd = string.Empty;
        Model = model;
        _existingFeatureFileViewModels = existingFeatureFileViewModels;
        OriginalModelState = Model.Clone();
        SelectedAttribute = null;
        IsVisible = true;
    }
}