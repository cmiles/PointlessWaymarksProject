using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using PointlessWaymarks.CmsData;
using PointlessWaymarks.CmsData.CommonHtml;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.ContentFormat;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using PointlessWaymarks.WpfCommon.WpfHtml;

namespace PointlessWaymarks.CmsWpfControls.BodyContentEditor;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class BodyContentEditorContext : IHasChanges, IHasValidationIssues, ICheckForChangesAndValidation
{
    private BodyContentEditorContext(StatusControlContext statusContext, IBodyContent dbEntry,
        ContentFormatChooserContext contentFormatChooser)
    {
        StatusContext = statusContext;

        DbEntry = dbEntry;

        BodyContentFormat = contentFormatChooser;

        BodyContentFormat.InitialValue = DbEntry.BodyContentFormat;

        UserValue = DbEntry.BodyContent;

        SelectedBodyText = string.Empty;

        BuildCommands();

        PropertyChanged += OnPropertyChanged;

        PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
    }

    public string? UserValue { get; set; }
    public ContentFormatChooserContext BodyContentFormat { get; set; }
    public bool BodyContentHasChanges { get; set; }
    public string? BodyContentHtmlOutput { get; set; }
    public IBodyContent? DbEntry { get; set; }
    public string? SelectedBodyText { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public int UserBodyContentUserSelectionLength { get; set; }
    public int UserBodyContentUserSelectionStart { get; set; }
    public string? UserHtmlSelectedText { get; set; }

    public void CheckForChangesAndValidationIssues()
    {
        BodyContentHasChanges = !StringTools.AreEqual((DbEntry?.BodyContent).TrimNullToEmpty(), UserValue);

        HasChanges = BodyContentHasChanges || PropertyScanners.ChildPropertiesHaveValidationIssues(this);
        HasValidationIssues = PropertyScanners.ChildPropertiesHaveChanges(this);
    }

    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }

    public static async Task<BodyContentEditorContext> CreateInstance(StatusControlContext? statusContext,
        IBodyContent dbEntry)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var factoryContext = statusContext ?? new StatusControlContext();

        var factoryChooser = await ContentFormatChooserContext.CreateInstance(factoryContext);

        var newContext = new BodyContentEditorContext(factoryContext, dbEntry, factoryChooser);

        var setUpdateFormatOk = await newContext.BodyContentFormat.TrySelectContentChoice(dbEntry.BodyContentFormat);

        if (!setUpdateFormatOk) newContext.StatusContext.ToastWarning("Trouble loading Format from Db...");

        newContext.CheckForChangesAndValidationIssues();

        return newContext;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (!e.PropertyName.Contains("HasChanges") && !e.PropertyName.Contains("Validation"))
            CheckForChangesAndValidationIssues();
    }

    [BlockingCommand]
    public async Task RefreshPreview()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        StatusContext.Progress("Building HTML");

        var settings = UserSettingsSingleton.CurrentSettings();

        try
        {
            var preprocessResults =
                await BracketCodeCommon.ProcessCodesForLocalDisplay(UserValue, StatusContext.ProgressTracker());
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
                $"<h2>Not able to process input</h2><p>{HttpUtility.HtmlEncode(e)}</p>".ToHtmlDocumentWithLeaflet(
                    "Invalid",
                    string.Empty);
        }
    }

    [BlockingCommand]
    private async Task RemoveLineBreaksFromSelected()
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (string.IsNullOrWhiteSpace(SelectedBodyText)) return;
        SelectedBodyText = Regex.Replace(SelectedBodyText, @"\r\n?|\n", " ");
        var options = RegexOptions.None;
        var regex = new Regex("[ ]{2,}", options);
        SelectedBodyText = regex.Replace(SelectedBodyText, " ");
    }

    [NonBlockingCommand]
    public async Task SpeakSelectedText()
    {
        await ThreadSwitcher.ResumeForegroundAsync();
        var speaker = new TextToSpeech();
        await speaker.Speak(UserHtmlSelectedText);
    }
}