using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MemoryLane.Windows;

public static class Branding
{
    public static ImageSource? LoadImage()
    {
        try
        {
            var resource = Application.GetResourceStream(
                new Uri("/Assets/MemoryLaneReferenceIcon.png", UriKind.Relative));
            if (resource?.Stream is null)
                return null;

            using var stream = resource.Stream;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex)
        {
            App.LogNonFatal("Branding.LoadImage", ex);
            return null;
        }
    }
}
