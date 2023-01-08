using System.Windows;
using System.Windows.Forms;
using Jot;

namespace PointlessWaymarks.CmsGui;

internal static class JotServices
{
    public static readonly Tracker Tracker = new();

    static JotServices()
    {
        Tracker.Configure<Window>().Id(w => w.Name, SystemInformation.VirtualScreen.Size)
            .Properties(w => new
            {
                w.Top,
                w.Width,
                w.Height,
                w.Left,
                w.WindowState
            }).PersistOn(nameof(Window.Closing)).StopTrackingOn(nameof(Window.Closing));
    }
}