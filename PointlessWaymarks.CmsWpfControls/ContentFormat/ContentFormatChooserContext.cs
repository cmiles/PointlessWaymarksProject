using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.ContentFormat;

public partial class ContentFormatChooserContext :  ObservableObject, IHasChanges, IHasValidationIssues
{
    [ObservableProperty] private List<ContentFormatEnum> _contentFormatChoices;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private string? _initialValue;
    private ContentFormatEnum _selectedContentFormat;
    [ObservableProperty] private string _selectedContentFormatAsString = string.Empty;
    [ObservableProperty] private bool _selectedContentFormatHasChanges;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _validationMessage = string.Empty;

    private ContentFormatChooserContext(StatusControlContext? statusContext)
    {
        _statusContext = statusContext ?? new StatusControlContext();
        _contentFormatChoices = Enum.GetValues(typeof(ContentFormatEnum)).Cast<ContentFormatEnum>().ToList();

        PropertyChanged += OnPropertyChanged;

        _selectedContentFormat = ContentFormatChoices.First();
    }

    public async Task CheckForChangesAndValidationIssues()
    {
        SelectedContentFormatHasChanges = InitialValue.TrimNullToEmpty() !=
                                          SelectedContentFormatAsString.TrimNullToEmpty();

        HasChanges = SelectedContentFormatHasChanges;
        var validation =
            await CommonContentValidation.ValidateBodyContentFormat(SelectedContentFormatAsString.TrimNullToEmpty());
        HasValidationIssues = !validation.Valid;
        ValidationMessage = validation.Explanation;
    }

    private async void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            await CheckForChangesAndValidationIssues();
    }

    public static async Task<ContentFormatChooserContext> CreateInstance(StatusControlContext statusContext)
    {
        ThreadSwitcher.ResumeBackgroundAsync();

        var toReturn = new ContentFormatChooserContext(statusContext);
        await toReturn.CheckForChangesAndValidationIssues();
        
        return toReturn;
    }


    public ContentFormatEnum SelectedContentFormat
    {
        get => _selectedContentFormat;
        set
        {
            if (value != _selectedContentFormat)
            {
                _selectedContentFormat = value;
                OnPropertyChanged();

                OnSelectedValueChanged?.Invoke(this,
                    Enum.GetName(typeof(ContentFormatEnum), SelectedContentFormat) ?? string.Empty);
            }

            SelectedContentFormatAsString =
                Enum.GetName(typeof(ContentFormatEnum), SelectedContentFormat) ?? string.Empty;
        }
    }

    public async Task<bool> TrySelectContentChoice(string? contentChoice)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(contentChoice))
        {
            SelectedContentFormat = ContentFormatDefaults.Content;
            return true;
        }

        var toSelect = Enum.TryParse(typeof(ContentFormatEnum), contentChoice, true, out var parsedSelection);
        if (toSelect && parsedSelection != null) SelectedContentFormat = (ContentFormatEnum)parsedSelection;
        return toSelect;
    }

    public event EventHandler<string>? OnSelectedValueChanged;
}