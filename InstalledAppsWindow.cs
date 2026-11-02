using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MemoryLane.Windows;

public sealed class InstalledAppsWindow : Window
{
    private readonly ObservableCollection<InstalledAppChoice> _items;
    private readonly ICollectionView _view;
    private readonly TextBlock _countText;

    public InstalledAppsWindow(PreferencesService preferences)
    {
        Title = "Exclude installed apps — Memory Trail";
        Width = 760;
        Height = 680;
        MinWidth = 620;
        MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(238, 244, 251));

        _items = new ObservableCollection<InstalledAppChoice>(
            InstalledAppService.Discover().Select(app => new InstalledAppChoice(app, preferences)));
        _view = CollectionViewSource.GetDefaultView(_items);

        var root = new Grid { Margin = new Thickness(22) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var headerCopy = new StackPanel();
        headerCopy.Children.Add(new TextBlock
        {
            Text = "Exclude installed apps",
            FontSize = 26,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(23, 35, 58))
        });
        headerCopy.Children.Add(new TextBlock
        {
            Text = "Selected apps are ignored for activity and clipboard capture.",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(102, 118, 139)),
            Margin = new Thickness(0, 5, 0, 0)
        });
        header.Children.Add(headerCopy);

        _countText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(65, 86, 113)),
            FontWeight = FontWeights.SemiBold
        };
        Grid.SetColumn(_countText, 1);
        header.Children.Add(_countText);
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var search = new TextBox
        {
            Height = 42,
            Padding = new Thickness(13, 9, 13, 9),
            FontSize = 14,
            BorderBrush = new SolidColorBrush(Color.FromRgb(213, 225, 238)),
            Background = Brushes.White,
            ToolTip = "Search by application or process name",
            Margin = new Thickness(0, 0, 0, 14)
        };
        search.TextChanged += (_, _) =>
        {
            var query = search.Text.Trim().ToLowerInvariant();
            _view.Filter = item => item is InstalledAppChoice choice &&
                (query.Length == 0 || choice.SearchText.Contains(query));
            _view.Refresh();
        };
        Grid.SetRow(search, 1);
        root.Children.Add(search);

        var list = new ListBox
        {
            ItemsSource = _view,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        list.ItemTemplate = CreateItemTemplate();
        list.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        Grid.SetRow(list, 2);
        root.Children.Add(list);

        var footer = new Grid { Margin = new Thickness(0, 16, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var privacy = new TextBlock
        {
            Text = "Passwords, credential prompts, 1Password, and Bitwarden remain protected by default.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(102, 118, 139)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 16, 0)
        };
        footer.Children.Add(privacy);

        var done = new Button
        {
            Content = "Done",
            MinWidth = 100,
            Padding = new Thickness(18, 10, 18, 10),
            Background = new SolidColorBrush(Color.FromRgb(30, 120, 240)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(30, 120, 240)),
            FontWeight = FontWeights.SemiBold
        };
        done.Click += (_, _) =>
        {
            preferences.Save();
            DialogResult = true;
            Close();
        };
        Grid.SetColumn(done, 1);
        footer.Children.Add(done);
        Grid.SetRow(footer, 3);
        root.Children.Add(footer);

        Content = root;
        UpdateCount();

        foreach (var item in _items)
            item.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(InstalledAppChoice.IsExcluded))
                    UpdateCount();
            };
    }

    private void UpdateCount()
    {
        var count = _items.Count(item => item.IsExcluded);
        _countText.Text = $"{count} excluded";
    }

    private static DataTemplate CreateItemTemplate()
    {
        var template = new DataTemplate(typeof(InstalledAppChoice));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, Brushes.White);
        border.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(216, 226, 239)));
        border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
        border.SetValue(Border.PaddingProperty, new Thickness(12, 9, 12, 9));
        border.SetValue(Border.MarginProperty, new Thickness(0, 0, 0, 6));

        var dock = new FrameworkElementFactory(typeof(DockPanel));
        dock.SetValue(DockPanel.LastChildFillProperty, true);

        var toggle = new FrameworkElementFactory(typeof(CheckBox));
        toggle.SetValue(DockPanel.DockProperty, Dock.Right);
        toggle.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
        toggle.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Right);
        toggle.SetValue(CheckBox.MarginProperty, new Thickness(12, 0, 0, 0));
        toggle.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(InstalledAppChoice.IsExcluded))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        dock.AppendChild(toggle);

        var image = new FrameworkElementFactory(typeof(Image));
        image.SetValue(DockPanel.DockProperty, Dock.Left);
        image.SetValue(Image.WidthProperty, 36d);
        image.SetValue(Image.HeightProperty, 36d);
        image.SetValue(Image.MarginProperty, new Thickness(0, 0, 12, 0));
        image.SetBinding(Image.SourceProperty, new Binding(nameof(InstalledAppChoice.Icon)));
        dock.AppendChild(image);

        var textStack = new FrameworkElementFactory(typeof(StackPanel));
        var name = new FrameworkElementFactory(typeof(TextBlock));
        name.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        name.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(23, 35, 58)));
        name.SetBinding(TextBlock.TextProperty, new Binding(nameof(InstalledAppChoice.DisplayName)));
        textStack.AppendChild(name);
        var process = new FrameworkElementFactory(typeof(TextBlock));
        process.SetValue(TextBlock.FontSizeProperty, 11d);
        process.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(102, 118, 139)));
        process.SetValue(TextBlock.MarginProperty, new Thickness(0, 2, 0, 0));
        process.SetBinding(TextBlock.TextProperty, new Binding(nameof(InstalledAppChoice.ProcessLabel)));
        textStack.AppendChild(process);
        dock.AppendChild(textStack);

        border.AppendChild(dock);
        template.VisualTree = border;
        return template;
    }
}

public sealed class InstalledAppChoice : INotifyPropertyChanged
{
    private readonly PreferencesService _preferences;
    private bool _isExcluded;

    public InstalledAppChoice(InstalledAppEntry app, PreferencesService preferences)
    {
        App = app;
        _preferences = preferences;
        _isExcluded = preferences.ExcludedProcesses.Contains(app.ProcessName);
        Icon = LoadIcon(app.ExecutablePath);
    }

    public InstalledAppEntry App { get; }
    public string DisplayName => App.DisplayName;
    public string ProcessLabel => $"{App.ProcessName}.exe";
    public string SearchText => App.SearchText;
    public ImageSource? Icon { get; }

    public bool IsExcluded
    {
        get => _isExcluded;
        set
        {
            if (_isExcluded == value) return;
            _isExcluded = value;
            if (value)
                _preferences.ExcludedProcesses.Add(App.ProcessName);
            else
                _preferences.ExcludedProcesses.Remove(App.ProcessName);
            OnPropertyChanged();
        }
    }

    private static ImageSource? LoadIcon(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            return null;

        try
        {
            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(executablePath);
            if (icon is null) return null;

            var source = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(48, 48));
            source.Freeze();
            return source;
        }
        catch
        {
            return null;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
