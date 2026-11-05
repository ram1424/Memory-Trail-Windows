using System.Windows;
using System.Windows.Media;

namespace MemoryLane.Windows;

public enum HistoryClearRange
{
    LastWeek,
    LastMonth,
    All
}

public partial class HistoryClearWindow : Window
{
    private readonly HistoryClearRange _range;

    public bool HistoryChanged { get; private set; }

    public HistoryClearWindow(HistoryClearRange range)
    {
        InitializeComponent();
        _range = range;

        try
        {
            var brand = Branding.LoadImage();
            if (brand is not null)
                Icon = brand;
        }
        catch (Exception ex)
        {
            App.LogNonFatal("HistoryClearWindow branding", ex);
        }

        Configure();
    }

    private void Configure()
    {
        var count = _range switch
        {
            HistoryClearRange.LastWeek => App.Store.CountSince(DateTime.Now.AddDays(-7)),
            HistoryClearRange.LastMonth => App.Store.CountSince(DateTime.Now.AddDays(-30)),
            _ => App.Store.Items.Count
        };

        CountText.Text = count == 1 ? "1 item will be removed." : count.ToString() + " items will be removed.";

        switch (_range)
        {
            case HistoryClearRange.LastWeek:
                GraphicGlyph.Text = "7";
                GraphicBadgeText.Text = "DAY SWEEP";
                PromptTitleText.Text = "Sweep the last week?";
                PromptBodyText.Text = "Seven days of footprints are about to vanish. Tiny broom, big responsibility.";
                ConfirmButton.Content = "Clear last week";
                break;

            case HistoryClearRange.LastMonth:
                GraphicGlyph.Text = "30";
                GraphicGlyph.FontSize = 37;
                GraphicGlyph.Foreground = new SolidColorBrush(Color.FromRgb(128, 87, 199));
                GraphicBadgeText.Text = "DAY SWEEP";
                PromptTitleText.Text = "Send the last month packing?";
                PromptBodyText.Text = "Thirty days are headed for the memory attic. No dramatic montage required.";
                ConfirmButton.Content = "Clear last month";
                break;

            default:
                GraphicGlyph.Text = "∞";
                GraphicGlyph.FontSize = 49;
                GraphicGlyph.Foreground = new SolidColorBrush(Color.FromRgb(212, 85, 85));
                GraphicBadgeText.Text = "BIG BROOM";
                PromptTitleText.Text = "Clear the entire trail?";
                PromptBodyText.Text = "This is the big red broom. Every saved footprint leaves the building. No undo, no tiny time machine.";
                ConfirmButton.Content = "Clear everything";
                break;
        }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        var removed = _range switch
        {
            HistoryClearRange.LastWeek => App.Store.ClearSince(DateTime.Now.AddDays(-7)),
            HistoryClearRange.LastMonth => App.Store.ClearSince(DateTime.Now.AddDays(-30)),
            _ => App.Store.ClearAllAndCount()
        };

        HistoryChanged = removed > 0;

        SuccessTitleText.Text = removed == 0 ? "Already spotless." : "Trail swept clean.";

        SuccessBodyText.Text = _range switch
        {
            HistoryClearRange.LastWeek when removed > 0 => "Last week is officially yesterday’s problem.",
            HistoryClearRange.LastMonth when removed > 0 => "A month of clutter just took a very long vacation.",
            HistoryClearRange.All when removed > 0 => "Clean slate achieved. Even the dust has been deleted.",
            _ => "There was nothing to remove. The broom is mildly disappointed."
        };

        RemovedText.Text = removed == 1 ? "1 item removed." : removed.ToString() + " items removed.";

        ConfirmPanel.Visibility = Visibility.Collapsed;
        SuccessPanel.Visibility = Visibility.Visible;
        ConfirmButton.Visibility = Visibility.Collapsed;
        CancelButton.Visibility = Visibility.Collapsed;
        DoneButton.Visibility = Visibility.Visible;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    private void Done_Click(object sender, RoutedEventArgs e) => Close();
}
