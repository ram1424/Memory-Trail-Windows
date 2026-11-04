using System.Windows;

namespace MemoryLane.Windows;

public partial class ClearCacheWindow : Window
{
    public bool ClearConfirmed { get; private set; }

    public ClearCacheWindow()
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
            App.LogNonFatal("ClearCacheWindow branding", ex);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => Close();

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        ClearConfirmed = true;
        DialogResult = true;
    }
}
