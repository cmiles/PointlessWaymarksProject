using System.Diagnostics;
using System.Windows;
using PointlessWaymarks.WpfCommon;

namespace PointlessWaymarks.CmsWpfControls.SitePictureSizesEditor;

/// <summary>
///     Interaction logic for SitePictureSizesEditorControl.xaml
/// </summary>
public partial class SitePictureSizesEditorControl
{
    private SitePictureSizesEditorContext? _editorContext;

    public SitePictureSizesEditorControl()
    {
        InitializeComponent();
    }

    private async void NewContextOnScrollIntoViewRequest(object? sender, ScrollSitePictureSizeIntoViewEventArgs e)
    {
        try
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            SitePictureSizesList.ScrollIntoView(e.ToScrollTo);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ignored Scroll To GUI Exception in {nameof(SitePictureSizesEditorControl)} - {ex}");
        }
    }

    private void SitePictureSizesEditorControl_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not SitePictureSizesEditorContext newContext) return;

        if (_editorContext is not null)
            try
            {
                _editorContext.ScrollIntoViewRequest -= NewContextOnScrollIntoViewRequest;
            }
            catch (Exception)
            {
                // ignored
            }

        _editorContext = newContext;
        newContext.ScrollIntoViewRequest += NewContextOnScrollIntoViewRequest;
    }
}