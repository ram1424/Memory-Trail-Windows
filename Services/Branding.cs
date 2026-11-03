using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MemoryLane.Windows;

public static class Branding
{
    private static ImageSource? _icon;

    public static ImageSource IconSource => _icon ??= LoadMasterIcon();

    public static ImageSource? LoadImage() => IconSource;

    public static System.Drawing.Icon CreateTrayIcon()
    {
        // Prefer the packaged multi-resolution ICO so the tray uses the same
        // approved artwork at native Windows icon sizes.
        try
        {
            var resource = Application.GetResourceStream(
                new Uri("pack://application:,,,/Assets/MemoryTrailApp.ico", UriKind.Absolute));

            if (resource?.Stream is not null)
            {
                using var stream = resource.Stream;
                using var icon = new System.Drawing.Icon(stream, 32, 32);
                return (System.Drawing.Icon)icon.Clone();
            }
        }
        catch (Exception ex)
        {
            App.LogNonFatal("Branding.CreateTrayIcon.ico", ex);
        }

        // If the ICO stream cannot be opened, render the exact packaged PNG.
        const int size = 64;
        var visual = new DrawingVisual();

        using (var dc = visual.RenderOpen())
            dc.DrawImage(IconSource, new Rect(0, 0, size, size));

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var pngStream = new MemoryStream();
        encoder.Save(pngStream);
        pngStream.Position = 0;

        using var sourceBitmap = new System.Drawing.Bitmap(pngStream);
        var hIcon = sourceBitmap.GetHicon();
        try
        {
            using var icon = System.Drawing.Icon.FromHandle(hIcon);
            return (System.Drawing.Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    private static ImageSource LoadMasterIcon()
    {
        Exception? lastError = null;

        // Absolute pack URIs are required here. The previous leading-slash
        // relative URI could fail in the published single-file app and caused
        // the blue-square/white-dot emergency fallback seen in the UI.
        foreach (var uriText in new[]
        {
            "pack://application:,,,/Assets/MemoryTrailIconMaster.png",
            "pack://application:,,,/Assets/MemoryTrailIcon.png",
            "pack://application:,,,/Assets/MemoryTrailApp.ico",
        })
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions =
                    BitmapCreateOptions.IgnoreImageCache |
                    BitmapCreateOptions.PreservePixelFormat;
                bitmap.UriSource = new Uri(uriText, UriKind.Absolute);
                bitmap.EndInit();

                if (bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
                    throw new InvalidOperationException($"Branding resource loaded with invalid dimensions: {uriText}");

                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex)
            {
                lastError = ex;
                App.LogNonFatal($"Branding.LoadMasterIcon {uriText}", ex);
            }
        }

        // Do not draw a substitute logo. If the approved artwork is missing,
        // surface the packaging error instead of silently showing the wrong icon.
        throw new InvalidOperationException(
            "The approved Memory Trail icon could not be loaded from packaged resources.",
            lastError);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
