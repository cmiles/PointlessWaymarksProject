using Microsoft.Toolkit.Mvvm.ComponentModel;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.CmsWpfControls.GpxImport;

/// <summary>
///     Interaction logic for GpxImportWindow.xaml
/// </summary>
[ObservableObject]
public partial class GpxImportWindow
{
    [ObservableProperty] private GpxImportContext _importContext;
    [ObservableProperty] private StatusControlContext _statusContext;

    public GpxImportWindow(string initialImportFile)
    {
        InitializeComponent();

        StatusContext = new StatusControlContext();

        DataContext = this;

        StatusContext.RunBlockingTask(async () =>
        {
            ImportContext = await GpxImportContext.CreateInstance(StatusContext);

            if (string.IsNullOrWhiteSpace(initialImportFile))
                await ImportContext.ChooseAndLoadFile();
            else
                await ImportContext.LoadFile(initialImportFile);
        });
    }
}