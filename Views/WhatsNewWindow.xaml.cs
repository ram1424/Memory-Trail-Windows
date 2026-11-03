using System.Diagnostics;
using System.Windows;

namespace MemoryLane.Windows;

public partial class WhatsNewWindow : Window
{
    public WhatsNewWindow()
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
            App.LogNonFatal("WhatsNewWindow branding", ex);
        }

        VersionBadgeText.Text = AppVersion.FriendlyVersion.ToUpperInvariant();
    }

    private void GotIt_Click(object sender, RoutedEventArgs e) => Close();

    private void ViewChangelog_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/ram1424/Memory-Trail-Windows/blob/main/docs/CHANGELOG.md",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            App.LogNonFatal("Open changelog", ex);
        }
    }
}
