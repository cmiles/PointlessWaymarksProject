using System.ComponentModel;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.Database;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.ContentFormat;

[NotifyPropertyChanged]
public partial class ContentFormatChooserContext : IHasChanges, IHasValidationIssues
{
    private ContentFormatEnum _selectedContentFormat;

    private ContentFormatChooserContext(StatusControlContext statusContext)
    {
        StatusContext = statusContext;
        ContentFormatChoices = Enum.GetValues(typeof(ContentFormatEnum)).Cast<ContentFormatEnum>().ToList();

        _selectedContentFormat = ContentFormatChoices.First();

        PropertyChanged += OnPropertyChanged;
    }

    public List<ContentFormatEnum> ContentFormatChoices { get; set; }
    public string? InitialValue { get; set; }

    [DoNotGenerateInpc]
    public ContentFormatEnum SelectedContentFormat
    {
        get => _selectedContentFormat;
        set
        {
            if (value != _selectedContentFormat)
            {
                _selectedContentFormat = value;
                OnPropertyChanged(nameof(SelectedContentFormat));

                OnSelectedValueChanged?.Invoke(this,
                    Enum.GetName(typeof(ContentFormatEnum), SelectedContentFormat) ?? string.Empty);
            }

            SelectedContentFormatAsString =
                Enum.GetName(typeof(ContentFormatEnum), SelectedContentFormat) ?? string.Empty;
        }
    }

    public string SelectedContentFormatAsString { get; set; } = string.Empty;
    public bool SelectedContentFormatHasChanges { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }

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

    public static async Task<ContentFormatChooserContext> CreateInstance(StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var toReturn = new ContentFormatChooserContext(statusContext);
        await toReturn.CheckForChangesAndValidationIssues();

        return toReturn;
    }

    private async void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            await CheckForChangesAndValidationIssues();
    }

    public event EventHandler<string>? OnSelectedValueChanged;

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
}