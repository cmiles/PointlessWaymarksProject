using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.ScreenShot
{
    //https://stackoverflow.com/questions/5124825/generating-a-screenshot-of-a-wpf-window
    public static class FrameworkElementScreenShot
    {
        private static async Task<bool> TryCopyBitmapToClipboard(BitmapSource bmpCopied)
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

        /// <summary>
        ///     Take screen shot of a Window.
        /// </summary>
        /// <remarks>
        ///     - Usage example: screen shot icon in every window header.
        ///     - Keep well away from any Windows Forms based methods that involve screen pixels. You will run into scaling issues
        ///     at different
        ///     monitor DPI values. Quote: "Keep in mind though that WPF units aren't pixels, they're device-independent @ 96DPI
        ///     "pixelish-units"; so really what you want, is the scale factor between 96DPI and the current screen DPI (so like
        ///     1.5 for
        ///     144DPI) - Paul Betts."
        /// </remarks>
        public static async Task<bool> TryScreenShotToClipboardAsync(FrameworkElement frameworkElement)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            //2020/5/6 - The line below is in the original stack overflow post but I had an error that this
            //call is not compatible with 'Window' and that is what I want to pass so commented out... original stack overflow comment
            //notes "hUses ClipToBounds so it is compatible with multiple docked windows in Infragistics."
            //frameworkElement.ClipToBounds = true; // Can remove if everything still works when the screen is maximized.

            var relativeBounds = VisualTreeHelper.GetDescendantBounds(frameworkElement);
            var areaWidth =
                frameworkElement.RenderSize
                    .Width; // Cannot use relativeBounds.Width as this may be incorrect if a window is maximized.
            var areaHeight = frameworkElement.RenderSize.Height; // Cannot use relativeBounds.Height for same reason.

            var xLeft = relativeBounds.X;
            var xRight = xLeft + areaWidth;
            var yTop = relativeBounds.Y;
            var yBottom = yTop + areaHeight;
            var bitmap = new RenderTargetBitmap((int) Math.Round(xRight, MidpointRounding.AwayFromZero),
                (int) Math.Round(yBottom, MidpointRounding.AwayFromZero), 96, 96, PixelFormats.Default);

            // Render framework element to a bitmap. This works better than any screen-pixel-scraping methods which will pick up unwanted
            // artifacts such as the taskbar or another window covering the current window.
            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                var vb = new VisualBrush(frameworkElement);
                ctx.DrawRectangle(vb, null, new Rect(new Point(xLeft, yTop), new Point(xRight, yBottom)));
            }

            bitmap.Render(dv);

            await ThreadSwitcher.ResumeBackgroundAsync();
            return await TryCopyBitmapToClipboard(bitmap);
        }
    }
}