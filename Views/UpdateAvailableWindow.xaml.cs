using System.Windows;

namespace MemoryLane.Windows;

public partial class UpdateAvailableWindow : Window
{
    private readonly UpdateCheckResult _result;

    public UpdateAvailableWindow(UpdateCheckResult result)
    {
        InitializeComponent();
        _result = result;

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
            App.LogNonFatal("UpdateAvailableWindow branding", ex);
        }

        ReleaseNameText.Text = result.ReleaseName;
        CurrentVersionText.Text = AppVersion.FriendlyVersion;
        LatestVersionText.Text = FriendlyReleaseName(result.ReleaseTag);
        ReleaseNotesText.Text = CleanReleaseNotes(result.ReleaseNotes);
    }

    private void Later_Click(object sender, RoutedEventArgs e) => Close();

    private void Download_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdateService.OpenDownload(_result);
            Close();
        }
        catch (Exception ex)
        {
            App.LogNonFatal("Open update download", ex);
            MessageBox.Show("Windows could not open the update download in your browser.", "Memory Trail Update",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private static string FriendlyReleaseName(string tag)
    {
        var value = tag.TrimStart('v', 'V');
        return $"Version {value}";
    }

    private static string CleanReleaseNotes(string notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return "This release contains improvements and fixes for Memory Trail.";

        var lines = notes.Replace("**", "")
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(line =>
            {
                var trimmed = line.Trim();
                while (trimmed.StartsWith("#", StringComparison.Ordinal))
                    trimmed = trimmed[1..].TrimStart();
                return trimmed;
            });

        return string.Join(Environment.NewLine, lines).Trim();
    }
}
