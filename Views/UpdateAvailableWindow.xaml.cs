using System.Windows;

namespace MemoryLane.Windows;

public enum UpdatePromptChoice
{
    None,
    Update,
    RemindLater,
    SkipVersion
}

public partial class UpdateAvailableWindow : Window
{
    private readonly UpdateCheckResult _result;

    public UpdatePromptChoice Choice { get; private set; }

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

        ReleaseNameText.Text = result.IsSameVersionRefresh
            ? $"{result.ReleaseName} has a newer build"
            : result.ReleaseName;

        CurrentVersionText.Text = AppVersion.FriendlyVersion;
        LatestVersionText.Text = FriendlyReleaseName(result.ReleaseTag);
        ReleaseNotesText.Text = CleanReleaseNotes(result.ReleaseNotes);
    }

    private void Update_Click(object sender, RoutedEventArgs e)
    {
        Choice = UpdatePromptChoice.Update;
        Close();
    }

    private void RemindLater_Click(object sender, RoutedEventArgs e)
    {
        Choice = UpdatePromptChoice.RemindLater;
        Close();
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        Choice = UpdatePromptChoice.SkipVersion;
        Close();
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
