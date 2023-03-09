using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentFormat;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WpfHtml;

namespace PointlessWaymarks.CmsWpfControls.BodyContentEditor;

public partial class BodyContentEditorContext : ObservableObject, IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation
{
    [ObservableProperty] private string? _bodyContent;
    [ObservableProperty] private ContentFormatChooserContext _bodyContentFormat;
    [ObservableProperty] private bool _bodyContentHasChanges;
    [ObservableProperty] private string? _bodyContentHtmlOutput;
    [ObservableProperty] private IBodyContent? _dbEntry;
    [ObservableProperty] private bool _hasChanges;
    [ObservableProperty] private bool _hasValidationIssues;
    [ObservableProperty] private RelayCommand _refreshPreviewCommand;
    [ObservableProperty] private RelayCommand _removeLineBreaksFromSelectedCommand;
    [ObservableProperty] private string? _selectedBodyText;
    [ObservableProperty] private RelayCommand _speakSelectedTextCommand;
    [ObservableProperty] private StatusControlContext _statusContext;
    [ObservableProperty] private string _userBodyContent = string.Empty;
    [ObservableProperty] private int _userBodyContentUserSelectionLength;
    [ObservableProperty] private int _userBodyContentUserSelectionStart;
    [ObservableProperty] private string? _userHtmlSelectedText;

    private BodyContentEditorContext(StatusControlContext statusContext, IBodyContent dbEntry, ContentFormatChooserContext contentFormatChooser)
    {
        _statusContext = statusContext;

        _removeLineBreaksFromSelectedCommand = StatusContext.RunBlockingActionCommand(RemoveLineBreaksFromSelected);
        _refreshPreviewCommand = StatusContext.RunBlockingTaskCommand(UpdateContentHtml);
        _speakSelectedTextCommand = StatusContext.RunNonBlockingTaskCommand(async () =>
        {
            await ThreadSwitcher.ResumeForegroundAsync();
            var speaker = new TextToSpeech();
            await speaker.Speak(UserHtmlSelectedText);
        });

        PropertyChanged += OnPropertyChanged;

        _dbEntry = dbEntry;

        _bodyContentFormat = contentFormatChooser;

        BodyContentFormat.InitialValue = _dbEntry.BodyContentFormat;

        _bodyContent = _dbEntry.BodyContent;

        _selectedBodyText = string.Empty;

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    public void CheckForChangesAndValidationIssues()
    {
        BodyContentHasChanges = !StringTools.AreEqual((DbEntry?.BodyContent).TrimNullToEmpty(), BodyContent);

        HasChanges = BodyContentHasChanges || PropertyScanners.ChildPropertiesHaveValidationIssues(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveChanges(this);
    }

    public static async Task<BodyContentEditorContext> CreateInstance(StatusControlContext? statusContext,
        IBodyContent dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();

        var factoryChooser = await ContentFormatChooserContext.CreateInstance(factoryContext);

        var newContext = new BodyContentEditorContext(factoryContext, dbEntry, factoryChooser);

        var setUpdateFormatOk = await newContext.BodyContentFormat.TrySelectContentChoice(dbEntry.BodyContentFormat);

        if (!setUpdateFormatOk) newContext.StatusContext.ToastWarning("Trouble loading Format from Db...");

        return newContext;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }

    private void RemoveLineBreaksFromSelected()
    {
        if (string.IsNullOrWhiteSpace(SelectedBodyText)) return;
        SelectedBodyText = Regex.Replace(SelectedBodyText, @"\r\n?|\n", " ");
        var options = RegexOptions.None;
        var regex = new Regex("[ ]{2,}", options);
        SelectedBodyText = regex.Replace(SelectedBodyText, " ");
    }

    public async Task UpdateContentHtml()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress("Building HTML");

        var settings = UserSettingsSingleton.CurrentSettings();

        try
        {
            var preprocessResults =
                await BracketCodeCommon.ProcessCodesForLocalDisplay(BodyContent, StatusContext.ProgressTracker());
            var processResults =
                ContentProcessing.ProcessContent(preprocessResults, BodyContentFormat.SelectedContentFormat);

            var possibleStyleFile = new FileInfo(Path.Combine(settings.LocalSiteDirectory().FullName, "style.css"));

            var styleBlock = "body { margin-right: 20px; }" + Environment.NewLine;

            if (possibleStyleFile.Exists) styleBlock += await File.ReadAllTextAsync(possibleStyleFile.FullName);

            BodyContentHtmlOutput = processResults.ToHtmlDocumentWithLeaflet("Body Content", styleBlock);
        }
        catch (Exception e)
        {
            BodyContentHtmlOutput =
                $"<h2>Not able to process input</h2><p>{HttpUtility.HtmlEncode(e)}</p>".ToHtmlDocumentWithLeaflet("Invalid",
                    string.Empty);
        }
    }
}