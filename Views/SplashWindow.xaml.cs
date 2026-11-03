using System.Windows;

namespace MemoryLane.Windows;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();

        try
        {
            var brand = Branding.LoadImage();
            if (brand is not null)
            {
                Icon = brand;
                BrandLogo.Source = brand;
            }
        }
        catch (Exception ex)
        {
            App.LogNonFatal("SplashWindow branding", ex);
        }
    }

    public void SetStatus(string message)
    {
        StatusText.Text = message;
    }
}
