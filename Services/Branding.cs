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
        const int size = 64;
        var visual = new DrawingVisual();

        using (var dc = visual.RenderOpen())
            dc.DrawImage(IconSource, new Rect(0, 0, size, size));

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        stream.Position = 0;

        using var sourceBitmap = new System.Drawing.Bitmap(stream);
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
        try
        {
            var resource = Application.GetResourceStream(
                new Uri("/Assets/MemoryTrailIconMaster.png", UriKind.Relative));

            if (resource?.Stream is null)
                throw new InvalidOperationException("MemoryTrailIconMaster.png was not found in app resources.");

            using var stream = resource.Stream;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bitmap.StreamSource = stream;
            bitmap.DecodePixelWidth = 512;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex)
        {
            App.LogNonFatal("Branding.LoadMasterIcon", ex);

            var fallback = new DrawingGroup();
            using (var dc = fallback.Open())
            {
                dc.DrawRoundedRectangle(
                    new SolidColorBrush(Color.FromRgb(31, 120, 240)),
                    null,
                    new Rect(0, 0, 64, 64),
                    14, 14);
                dc.DrawEllipse(Brushes.White, null, new Point(32, 32), 12, 12);
            }

            fallback.Freeze();
            var image = new DrawingImage(fallback);
            image.Freeze();
            return image;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
