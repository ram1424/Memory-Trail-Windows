using System.Windows;
using System.Windows.Controls;

namespace MemoryLane.Windows;

public partial class AddMemoryWindow : Window
{
    public MemoryItem? CreatedItem { get; private set; }

    public AddMemoryWindow()
    {
        InitializeComponent();
        TitleBox.ToolTip = "Title";
        BodyBox.ToolTip = "What do you want to remember?";
        UrlBox.ToolTip = "Optional URL";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show("Enter a title first.", "Memory Lane");
            return;
        }

        var kindText = (KindBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Note";
        _ = Enum.TryParse<SourceKind>(kindText, out var kind);

        CreatedItem = new MemoryItem
        {
            Title = title,
            Body = BodyBox.Text.Trim(),
            SourceApp = "Memory Lane",
            SourceKind = kind,
            SourceUrl = string.IsNullOrWhiteSpace(UrlBox.Text) ? null : UrlBox.Text.Trim()
        };

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
