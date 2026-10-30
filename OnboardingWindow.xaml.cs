using System.Windows;
using System.Windows.Media;

namespace MemoryLane.Windows;

public partial class OnboardingWindow : Window
{
    private int _page = 1;

    public OnboardingWindow()
    {
        InitializeComponent();

        try
        {
            var brand = Branding.LoadImage();
            if (brand is not null)
            {
                Icon = brand;
                WelcomeBrandLogo.Source = brand;
            }
        }
        catch (Exception ex)
        {
            App.LogNonFatal("Onboarding branding", ex);
        }

        UpdatePage();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_page > 1)
        {
            _page--;
            UpdatePage();
        }
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_page < 3)
        {
            _page++;
            UpdatePage();
            return;
        }

        DialogResult = true;
    }

    private void UpdatePage()
    {
        Page1.Visibility = _page == 1 ? Visibility.Visible : Visibility.Collapsed;
        Page2.Visibility = _page == 2 ? Visibility.Visible : Visibility.Collapsed;
        Page3.Visibility = _page == 3 ? Visibility.Visible : Visibility.Collapsed;

        BackButton.Visibility = _page == 1 ? Visibility.Hidden : Visibility.Visible;
        NextButton.Content = _page == 3 ? "Get Started" : "Next  →";

        var active = new SolidColorBrush(Color.FromRgb(31, 143, 255));
        var inactive = new SolidColorBrush(Color.FromRgb(52, 74, 105));

        Dot1.Fill = _page == 1 ? active : inactive;
        Dot2.Fill = _page == 2 ? active : inactive;
        Dot3.Fill = _page == 3 ? active : inactive;
    }
}
