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
                new Uri("/Assets/MemoryTrailIconMaster.png", UriKind.Relative));
            if (resource?.Stream is null)
                return null;

            using var stream = resource.Stream;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            _cached = bitmap;
            return _cached;
        }
        catch (Exception ex)
        {
            App.LogNonFatal("Branding.LoadImage", ex);
            return null;
        }
    }
}
