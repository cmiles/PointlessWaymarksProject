// Code based on:
// Capturing screenshots using C# and p/invoke
// http://www.cyotek.com/blog/capturing-screenshots-using-csharp-and-p-invoke
// ReSharper disable once CommentTypo
// Copyright © 2017-2019 Cyotek Ltd. All Rights Reserved.

// This work is licensed under the Creative Commons Attribution 4.0 International License.
// To view a copy of this license, visit http://creativecommons.org/licenses/by/4.0/.

using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace PointlessWaymarks.WpfCommon.WindowScreenShot;

public static class NativeCapture
{
    //https://stackoverflow.com/questions/1118496/using-image-control-in-wpf-to-display-system-drawing-bitmap/1118557#1118557
    public static BitmapSource BitmapSourceFromSystemDrawingBitmap(Bitmap source)
    {
        var ip = source.GetHbitmap();
        BitmapSource bs;
        try
        {
            bs = Imaging.CreateBitmapSourceFromHBitmap(ip, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            NativeScreenShotMethods.DeleteObject(ip);
        }

        return bs;
    }

    public static Bitmap CaptureActiveWindow()
    {
        return CaptureWindow(NativeScreenShotMethods.GetForegroundWindow());
    }

    public static Bitmap CaptureRegion(Rect region)
    {
        Bitmap result;

        // ReSharper disable once IdentifierTypo
        var desktophWnd = NativeScreenShotMethods.GetDesktopWindow();
        var desktopDc = NativeScreenShotMethods.GetWindowDC(desktophWnd);
        var memoryDc = NativeScreenShotMethods.CreateCompatibleDC(desktopDc);
        var bitmap =
            NativeScreenShotMethods.CreateCompatibleBitmap(desktopDc, (int) region.Width, (int) region.Height);
        var oldBitmap = NativeScreenShotMethods.SelectObject(memoryDc, bitmap);

        var success = NativeScreenShotMethods.BitBlt(memoryDc, 0, 0, (int) region.Width, (int) region.Height,
            desktopDc, (int) region.Left, (int) region.Top,
            NativeScreenShotMethods.RasterOperations.SRCCOPY | NativeScreenShotMethods.RasterOperations.CAPTUREBLT);

        try
        {
            if (!success) throw new Win32Exception();

            result = Image.FromHbitmap(bitmap);
        }
        finally
        {
            NativeScreenShotMethods.SelectObject(memoryDc, oldBitmap);
            NativeScreenShotMethods.DeleteObject(bitmap);
            NativeScreenShotMethods.DeleteDC(memoryDc);
            NativeScreenShotMethods.ReleaseDC(desktophWnd, desktopDc);
        }

        return result;
    }

    public static Bitmap CaptureWindow(IntPtr hWnd)
    {
        NativeScreenShotMethods.RECT region;

        if (Environment.OSVersion.Version.Major < 6)
        {
            NativeScreenShotMethods.GetWindowRect(hWnd, out region);
        }
        else
        {
            if (NativeScreenShotMethods.DwmGetWindowAttribute(hWnd,
                    NativeScreenShotMethods.DWMWA_EXTENDED_FRAME_BOUNDS, out region,
                    Marshal.SizeOf(typeof(NativeScreenShotMethods.RECT))) !=
                0) NativeScreenShotMethods.GetWindowRect(hWnd, out region);
        }

        return CaptureRegion(new Rect(new Point(region.left, region.top),
            new Size(region.right - region.left, region.bottom - region.top)));
    }

    public static Bitmap CaptureWindow(Window wpfWindow)
    {
        return CaptureWindow(new WindowInteropHelper(wpfWindow).Handle);
    }

    /// <summary>
    ///     Take screen shot of a Window.
    /// </summary>
    public static async Task<bool> TryActiveWindowScreenShotToClipboardAsync()
    {
        return await TryCopyBitmapSourceToClipboard(CaptureActiveWindow());
    }

    private static async Task<bool> TryCopyBitmapSourceToClipboard(Bitmap bmpCopied)
    {
        return await TryCopyBitmapSourceToClipboard(BitmapSourceFromSystemDrawingBitmap(bmpCopied));
    }

    private static async Task<bool> TryCopyBitmapSourceToClipboard(BitmapSource bmpCopied)
    {
        var tries = 3;
        while (tries-- > 0)
            try
            {
                await ThreadSwitcher.ResumeForegroundAsync();
                // This must be executed on the calling dispatcher.
                Clipboard.SetImage(bmpCopied);
                return true;
            }
            catch (COMException)
            {
                // Windows clipboard is optimistic concurrency. On fail (as in use by another process), retry.
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

        return false;
    }

    public static async Task<bool> TryWindowScreenShotToClipboardAsync(Window x)
    {
        return await TryCopyBitmapSourceToClipboard(CaptureWindow(x));
    }
}