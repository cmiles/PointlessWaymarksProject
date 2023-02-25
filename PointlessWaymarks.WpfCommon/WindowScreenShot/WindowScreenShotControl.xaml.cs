using System.Windows;
using CommunityToolkit.Mvvm.Input;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.WpfCommon.WindowScreenShot;

/// <summary>
///     Interaction logic for WindowScreenShotControl.xaml
/// </summary>
public partial class WindowScreenShotControl
{
    public WindowScreenShotControl()
    {
        InitializeComponent();

        WindowScreenShotCommand = new AsyncRelayCommand<Window>(async x =>
        {
            if (x == null) return;

            StatusControlContext? statusContext = null;

            try
            {
                statusContext = (StatusControlContext)((dynamic)x.DataContext).StatusContext;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            var result = await NativeCapture.TryWindowScreenShotToClipboardAsync(x);

            if (statusContext != null)
            {
                if (result)
                    statusContext.ToastSuccess("Window copied to Clipboard");
                else
                    statusContext.ToastError("Problem Copying Window to Clipboard");
            }
        });

        DataContext = this;
    }

    public AsyncRelayCommand<Window> WindowScreenShotCommand { get; }
}