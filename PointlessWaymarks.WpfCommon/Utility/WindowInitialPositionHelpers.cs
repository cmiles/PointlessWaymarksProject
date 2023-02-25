using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using WpfScreenHelper;
using Point = System.Drawing.Point;

namespace PointlessWaymarks.WpfCommon.Utility;

/// <summary>
/// Methods for 'safely' positioning a window to avoid pitfalls like the window being
/// off screen. Heavily based on reading code from https://github.com/RickStrahl/MarkdownMonster/,
/// https://github.com/anakic/Jot, https://github.com/microsoft/WPF-Samples/tree/master/Windows/SaveWindowState
/// and https://github.com/micdenny/WpfScreenHelper
/// </summary>
public static class WindowInitialPositionHelpers
{
    /// <summary>
    /// !! Only call this on the UI Thread !! Ensures that a window is visible on a screen (ie the window is not trapped off screen).
    /// In general for anything other than the initial main window try to use PositionWindowAndShowOnUiThread
    /// as it will both ensure calls are on the correct thread and will by default
    /// position windows relative to the first active window in the application.
    /// </summary>
    /// <param name="window"></param>
    public static void EnsureWindowIsVisible(Window window)
    {
        //This borrows heavily from https://github.com/RickStrahl/MarkdownMonster/

        if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;

        var hwnd = WindowToHwnd(window);
        var screenBounds = Screen.FromHandle(new WindowInteropHelper(window).Handle).WorkingArea;

        var screenWidth = screenBounds.Width;
        var screenHeight = screenBounds.Height;
        var screenX = screenBounds.X;
        var screenY = screenBounds.Y;

        var windowLeftAbsolute = window.Left + screenX; // absolute Left
        var windowTopAbsolute = window.Top + screenY; // absolute Top

        if (window.Left + window.Width > screenWidth)
        {
            window.Left = screenX + screenWidth - window.Width;
            windowLeftAbsolute = window.Left;
        }

        if (windowLeftAbsolute < screenX)
        {
            window.Left = 10 + screenX;
            if (window.Width + 10 > screenWidth)
                window.Width = screenWidth - 20;
        }

        if (window.Top + window.Height > screenHeight)
        {
            window.Top = screenY + screenHeight - window.Height;
            windowTopAbsolute = window.Top;
        }

        if (windowTopAbsolute < screenY)
        {
            window.Top = 10 + screenY;
            if (window.Height + 10 > screenHeight)
                window.Height = screenHeight - 20;
        }
    }

    private static uint GetDpi(IntPtr hwnd, DpiType dpiType)
    {
        var screen = Screen.FromHandle(hwnd);
        var screenPoint = new Point((int)screen.Bounds.Left, (int)screen.Bounds.Top);
        var monitor = MonitorFromPoint(screenPoint, 2 /*MONITOR_DEFAULTTONEAREST*/);

        uint dpiX = 96;

        try
        {
            GetDpiForMonitor(monitor, dpiType, out dpiX, out _);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return dpiX;
    }

    //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx
    [DllImport("Shcore.dll")]
    private static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiType dpiType, [Out] out uint dpiX,
        [Out] out uint dpiY);

    private static decimal GetDpiRatio(IntPtr hwnd)
    {
        var dpi = GetDpi(hwnd, DpiType.Effective);

        if (dpi > 96) return dpi / 96M;

        return 1;
    }

    //https://msdn.microsoft.com/en-us/library/windows/desktop/dd145062(v=vs.85).aspx
    [DllImport("User32.dll")]
    private static extern IntPtr MonitorFromPoint([In] Point pt, [In] uint dwFlags);


    /// <summary>
    ///     !! Only call this on the UI Thread !! Positions a window attempting to avoid common pitfalls like being offscreen
    ///     - this avoids the need to Dispatch this to or
    ///     switch to the UI thread before interacting with the window.
    /// </summary>
    /// <param name="toPosition">If null the position is based on the first active window in the Application</param>
    /// <returns></returns>
    public static void PositionWindowAndShow(this Window toPosition)
    {
        var positionReference = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive) ??
                                Application.Current.Windows.OfType<Window>().FirstOrDefault();

        if (positionReference != null)
        {
            toPosition.Left = positionReference.Left + 10;
            toPosition.Top = positionReference.Top + 24;
        }

        EnsureWindowIsVisible(toPosition);

        toPosition.Show();
    }

    /// <summary>
    ///     Positions a window on the UI Thread attempting to avoid common pitfalls like being offscreen
    ///     - this avoids the need to Dispatch this to or
    ///     switch to the UI thread before interacting with the window.
    /// </summary>
    /// <param name="toPosition">If null the position is based on the first active window in the Application</param>
    /// <returns></returns>
    public static async Task PositionWindowAndShowOnUiThread(this Window toPosition)
    {
        await ThreadSwitcher.ThreadSwitcher.ResumeForegroundAsync();

        toPosition.PositionWindowAndShow();
    }

    private static IntPtr WindowToHwnd(Window window)
    {
        return new WindowInteropHelper(window).EnsureHandle();
    }

    private enum DpiType
    {
        Effective = 0,
        Angular = 1,
        Raw = 2
    }
}