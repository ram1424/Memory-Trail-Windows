using System.Windows;

namespace MemoryLane.Windows;

public partial class PrivacyPolicyWindow : Window
{
    public PrivacyPolicyWindow()
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
            App.LogNonFatal("PrivacyPolicyWindow branding", ex);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();
}
