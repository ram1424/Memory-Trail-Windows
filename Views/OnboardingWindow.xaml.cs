using System.Windows;
using System.Windows.Media;

namespace MemoryLane.Windows;

public partial class OnboardingWindow : Window
{
    private const int PageCount = 6;
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
                ContextBrandLogo.Source = brand;
            }

            EdgeTutorialIcon.Source = AppIconConverter.ResolveAppIcon("Microsoft Edge", "msedge");
            OutlookTutorialIcon.Source = AppIconConverter.ResolveAppIcon("Outlook", "olk");
        }
        catch (Exception ex)
        {
            App.LogNonFatal("Onboarding branding", ex);
        }

        UpdatePage();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_page <= 1) return;
        _page--;
        UpdatePage();
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_page < PageCount)
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
        Page4.Visibility = _page == 4 ? Visibility.Visible : Visibility.Collapsed;
        Page5.Visibility = _page == 5 ? Visibility.Visible : Visibility.Collapsed;
        Page6.Visibility = _page == 6 ? Visibility.Visible : Visibility.Collapsed;

        BackButton.Visibility = _page == 1 ? Visibility.Hidden : Visibility.Visible;
        NextButton.Content = _page == PageCount ? "Start Using Memory Trail  ✓" : "Continue  →";

        PageCounterText.Text = $"{_page} of {PageCount}";
        PageTitleTopText.Visibility = _page is > 1 and < PageCount ? Visibility.Visible : Visibility.Collapsed;
        PageTitleTopText.Text = _page switch
        {
            2 => "Today",
            3 => "Ask Memory Trail",
            4 => "Capture & Files",
            5 => "Privacy & Controls",
            6 => "Ready to Start",
            _ => "Welcome"
        };

        var active = new SolidColorBrush(Color.FromRgb(22, 128, 255));
        var inactive = new SolidColorBrush(Color.FromRgb(189, 201, 217));

        var dots = new[] { Dot1, Dot2, Dot3, Dot4, Dot5, Dot6 };
        for (var i = 0; i < dots.Length; i++)
            dots[i].Fill = i == _page - 1 ? active : inactive;
    }
}
