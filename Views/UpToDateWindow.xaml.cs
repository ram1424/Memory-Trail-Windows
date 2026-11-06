using System.Diagnostics;
using System.Windows;

namespace MemoryLane.Windows;

public partial class UpToDateWindow : Window
{
    private readonly bool _storeManaged;

    public UpToDateWindow(bool storeManaged = false)
    {
        InitializeComponent();
        _storeManaged = storeManaged;

        VersionText.Text = AppVersion.FriendlyVersion;

        if (storeManaged)
        {
            HeadlineText.Text = "Microsoft Store has the keys.";
            SubtitleText.Text = "You’re all set. Microsoft Store handles this copy’s updates while Memory Trail keeps your trail moving.";
            StatusBadgeText.Text = "Store Managed";
            PunchlineText.Text = "Everything is current. The trail is clear, the mountains are calm, and the Store pit crew has update duty covered.";
            PrimaryActionButton.Content = "Open Store Updates";
        }

        try
        {
            var brand = Branding.LoadImage();
            if (brand is not null)
            {
                Icon = brand;
                HeaderLogo.Source = brand;
                CardLogo.Source = brand;
            }
        }
        catch (Exception ex)
        {
            App.LogNonFatal("UpToDateWindow branding", ex);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (_storeManaged)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-windows-store://downloadsandupdates",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                App.LogNonFatal("Open Microsoft Store updates", ex);
            }
        }

        Close();
    }
}
