using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Content;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.ContentFormat;

public partial class ContentFormatChooserContext :  ObservableObject, IHasChanges, IHasValidationIssues
{
    [ObservableProperty] private List<ContentFormatEnum> _contentFormatChoices;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private string _initialValue;
    private ContentFormatEnum _selectedContentFormat;
    [ObservableProperty] private string _selectedContentFormatAsString;
    [ObservableProperty] private bool _selectedContentFormatHasChanges;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _validationMessage;

    private ContentFormatChooserContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext ?? new StatusControlContext();
        ContentFormatChoices = Enum.GetValues(typeof(ContentFormatEnum)).Cast<ContentFormatEnum>().ToList();

        PropertyChanged += OnPropertyChanged;

        SelectedContentFormat = ContentFormatChoices.First();
    }

    public async Task CheckForChangesAndValidationIssues()
    {
        // ReSharper disable InvokeAsExtensionMethod - in this case TrimNullSage - which returns an
        //Empty string from null will not be invoked as an extension if DbEntry is null...
        SelectedContentFormatHasChanges = StringTools.TrimNullToEmpty(InitialValue) !=
                                          SelectedContentFormatAsString.TrimNullToEmpty();
        // ReSharper restore InvokeAsExtensionMethod

        HasChanges = SelectedContentFormatHasChanges;
        var validation =
            await CommonContentValidation.ValidateBodyContentFormat(SelectedContentFormatAsString.TrimNullToEmpty());
        HasValidationIssues = !validation.Valid;
        ValidationMessage = validation.Explanation;
    }

    private async void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            await CheckForChangesAndValidationIssues();
    }

    public static ContentFormatChooserContext CreateInstance(StatusControlContext statusContext)
    {
        return new(statusContext);
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

                OnSelectedValueChanged?.Invoke(this, Enum.GetName(typeof(ContentFormatEnum), SelectedContentFormat));
            }

            SelectedContentFormatAsString =
                Enum.GetName(typeof(ContentFormatEnum), SelectedContentFormat) ?? string.Empty;
        }
    }

    public async Task<bool> TrySelectContentChoice(string contentChoice)
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

    public event EventHandler<string> OnSelectedValueChanged;
}