using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using PointlessWaymarks.CmsData.Database;
using WpfScreenHelper;
using Point = System.Drawing.Point;

namespace PointlessWaymarks.CmsWpfControls.Utility
{
    public static class WindowInitialPositionHelpers
    {
        //This file is based on:
        //https://github.com/RickStrahl/MarkdownMonster/
        //https://github.com/anakic/Jot
        //https://github.com/microsoft/WPF-Samples/tree/master/Windows/SaveWindowState
        //https://github.com/micdenny/WpfScreenHelper
        //
        //This borrows heavily esp. with EnsureWindowIsVisible being nearly a copy of the Markdown Monster with only a 
        //few changes.
        public enum DpiType
        {
            Effective = 0,
            Angular = 1,
            Raw = 2
        }

        public static void EnsureWindowIsVisible(Window window)
        {
            if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;

            var hwnd = WindowToHwnd(window);
            var screenBounds = Screen.FromHandle(new WindowInteropHelper(window).Handle).Bounds;
            var ratio = Convert.ToDouble(GetDpiRatio(hwnd));

            var screenWidth = screenBounds.Width / ratio;
            var screenHeight = screenBounds.Height / ratio;
            var screenX = screenBounds.X / ratio;
            var screenY = screenBounds.Y / ratio;

            var windowLeftAbsolute = window.Left + screenX; // absolute Left
            var windowTopAbsolute = window.Top + screenY; // absolute Top

            if (window.Left + window.Width > screenWidth * 1.1)
            {
                window.Left = screenX + screenWidth - window.Width;
                windowLeftAbsolute = window.Left;
            }

            if (windowLeftAbsolute < screenX)
            {
                window.Left = 20 + screenX;
                if (window.Width + 20 > screenWidth)
                    window.Width = screenWidth - 40;
            }

            if (window.Top + window.Height > screenHeight)
            {
                window.Top = screenY + screenHeight - window.Height;
                windowTopAbsolute = window.Top;
            }

            if (windowTopAbsolute < screenY)
            {
                window.Top = 20 + screenY;
                if (window.Height + 20 > screenHeight)
                    window.Height = screenHeight - 60;
            }
        }

        public static uint GetDpi(IntPtr hwnd, DpiType dpiType)
        {
            var screen = Screen.FromHandle(hwnd);
            var screenPoint = new Point((int) screen.Bounds.Left, (int) screen.Bounds.Top);
            var monitor = MonitorFromPoint(screenPoint, 2 /*MONITOR_DEFAULTTONEAREST*/);

            uint dpiX = 96;

            try
            {
                GetDpiForMonitor(monitor, dpiType, out dpiX, out _);
            }
            catch (Exception ex)
            {
                EventLogContext.TryWriteExceptionToLogBlocking(ex, "WindowInitialPositionHelpers",
                    "GetDpi failed in GetDpiForMonitor");
            }

            return dpiX;
        }

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx
        [DllImport("Shcore.dll")]
        private static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiType dpiType, [Out] out uint dpiX,
            [Out] out uint dpiY);

        public static decimal GetDpiRatio(IntPtr hwnd)
        {
            var dpi = GetDpi(hwnd, DpiType.Effective);

            if (dpi > 96) return dpi / 96M;

            return 1;
        }

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dd145062(v=vs.85).aspx
        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromPoint([In] Point pt, [In] uint dwFlags);

        public static void PositionWindowAndShow(this Window toPosition)
        {
            if (toPosition == null) return;

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

        public static IntPtr WindowToHwnd(Window window)
        {
            return new WindowInteropHelper(window).EnsureHandle();
        }
    }
}