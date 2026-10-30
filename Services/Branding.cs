using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MemoryLane.Windows;

public static class Branding
{
    private static ImageSource? _cached;
    private static bool _attempted;

    public static ImageSource? LoadImage()
    {
        if (_attempted)
            return _cached;

        _attempted = true;

        try
        {
            var resource = Application.GetResourceStream(
                new Uri("/Assets/MemoryTrailApp.ico", UriKind.Relative));
            if (resource?.Stream is null)
                return null;

            using var stream = resource.Stream;
            var decoder = new IconBitmapDecoder(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);

            var frame = decoder.Frames
                .Where(f => f.PixelWidth <= 256 && f.PixelHeight <= 256)
                .OrderByDescending(f => f.PixelWidth * f.PixelHeight)
                .FirstOrDefault()
                ?? decoder.Frames.OrderByDescending(f => f.PixelWidth * f.PixelHeight).FirstOrDefault();

            if (frame is null)
                return null;

            frame.Freeze();
            _cached = frame;
            return _cached;
        }
        catch (Exception ex)
        {
            App.LogNonFatal("Branding.LoadImage", ex);
            return null;
        }
    }
}
