using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.CmsData.Database.Models;
using PointlessWaymarks.CmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.CmsWpfControls.PointContentEditor;

/// <summary>
///     Interaction logic for PointContentEditorWindow.xaml
/// </summary>
[ObservableObject]
public partial class PointContentEditorWindow
{
    [ObservableProperty] private WindowAccidentalClosureHelper _accidentalCloserHelper;
    [ObservableProperty] private PointContentEditorContext _pointContent;
    [ObservableProperty] private StatusControlContext _statusContext;

    public PointContentEditorWindow(PointContent toLoad)
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            PointContent = await PointContentEditorContext.CreateInstance(StatusContext, toLoad);

            PointContent.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, PointContent);

            await ThreadSwitcher.ResumeForegroundAsync();
            DataContext = this;
        });
    }

    public PointContentEditorWindow(double latitude, double longitude, double? elevation, string initialTitleText,
        string initialBodyText)
    {
        InitializeComponent();
        StatusContext = new StatusControlContext();

        StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            PointContent = await PointContentEditorContext.CreateInstance(StatusContext, null);

            PointContent.RequestContentEditorWindowClose += (_, _) => { Dispatcher?.Invoke(Close); };
            AccidentalCloserHelper = new WindowAccidentalClosureHelper(this, StatusContext, PointContent);

            if (!string.IsNullOrWhiteSpace(initialTitleText))
                PointContent.TitleSummarySlugFolder.TitleEntry.UserValue = initialTitleText;
            if (!string.IsNullOrWhiteSpace(initialBodyText)) PointContent.BodyContent.BodyContent = initialBodyText;

            PointContent.LatitudeEntry.UserText = latitude.ToString("F6");
            PointContent.LongitudeEntry.UserText = longitude.ToString("F6");
            if (elevation != null) PointContent.ElevationEntry.UserText = elevation.Value.ToString("F2");

            await ThreadSwitcher.ResumeForegroundAsync();
            DataContext = this;
        });
    }
}