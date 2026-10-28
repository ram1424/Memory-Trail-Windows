using System.Diagnostics;
using System.Windows;

namespace MemoryLane.Windows;

public partial class MemoryDetailWindow : Window
{
    private readonly MemoryItem _item;

    public MemoryDetailWindow(MemoryItem item)
    {
        InitializeComponent();
        _item = item;

        TimestampText.Text = item.CreatedAt.ToString("dddd, MMMM d, yyyy 'at' h:mm:ss tt");
        SourceAppText.Text = string.IsNullOrWhiteSpace(item.SourceApp) ? "Application" : item.SourceApp;
        TitleText.Text = string.IsNullOrWhiteSpace(item.Title) ? "Untitled activity" : item.Title;
        BodyText.Text = string.IsNullOrWhiteSpace(item.Body) ? "No additional details were captured for this activity." : item.Body;
        KindText.Text = item.SourceKind.ToString();
        ProcessText.Text = string.IsNullOrWhiteSpace(item.SourceProcess) ? "Not available" : item.SourceProcess;

        if (string.IsNullOrWhiteSpace(item.SourceUrl))
        {
            UrlText.Text = "No source link was captured.";
            OpenSourceButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            UrlText.Text = item.SourceUrl;
            OpenSourceButton.Visibility = Visibility.Visible;
        }
    }

    private void OpenSource_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_item.SourceUrl))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _item.SourceUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            MessageBox.Show("Windows could not open this source.", "Memory Trail", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
