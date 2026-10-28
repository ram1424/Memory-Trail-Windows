using System.Windows;

namespace MemoryLane.Windows;

public partial class OnboardingWindow : Window
{
    public OnboardingWindow() => InitializeComponent();

    private void GetStarted_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
