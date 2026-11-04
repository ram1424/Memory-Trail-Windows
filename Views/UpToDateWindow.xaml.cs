using System.Windows;

namespace MemoryLane.Windows;

public partial class UpToDateWindow : Window
{
    public UpToDateWindow()
    {
        InitializeComponent();

        VersionText.Text = AppVersion.FriendlyVersion;

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
        => Close();
}
