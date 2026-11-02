using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace MemoryLane.Windows;

public sealed class CategoryIcon : FrameworkElement
{
    public static readonly DependencyProperty CategoryProperty =
        DependencyProperty.Register(
            nameof(Category),
            typeof(string),
            typeof(CategoryIcon),
            new FrameworkPropertyMetadata("Other", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SoftBackgroundProperty =
        DependencyProperty.Register(
            nameof(SoftBackground),
            typeof(bool),
            typeof(CategoryIcon),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public string Category
    {
        get => (string)GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    public bool SoftBackground
    {
        get => (bool)GetValue(SoftBackgroundProperty);
        set => SetValue(SoftBackgroundProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsNaN(Width) ? 28 : Width;
        var height = double.IsNaN(Height) ? 28 : Height;
        return new Size(width, height);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var w = Math.Max(1, ActualWidth);
        var h = Math.Max(1, ActualHeight);
        var size = Math.Min(w, h);
        var x = (w - size) / 2;
        var y = (h - size) / 2;
        var tile = new Rect(x, y, size, size);

        var kind = Normalize(Category);
        var (iconColor, backgroundColor) = ColorsFor(kind);
        var iconBrush = new SolidColorBrush(iconColor);
        iconBrush.Freeze();

        if (SoftBackground)
        {
            var shadowColor = Color.FromArgb(34, iconColor.R, iconColor.G, iconColor.B);
            var shadowBrush = new SolidColorBrush(shadowColor);
            shadowBrush.Freeze();

            var shadow = new Rect(tile.X, tile.Y + size * 0.055, tile.Width, tile.Height);
            dc.DrawRoundedRectangle(shadowBrush, null, shadow, size * 0.30, size * 0.30);

            var top = Lighten(backgroundColor, 0.52);
            var bottom = Lighten(backgroundColor, 0.10);
            var backgroundBrush = new LinearGradientBrush(top, bottom, new Point(0, 0), new Point(1, 1));
            backgroundBrush.Freeze();

            var borderColor = Color.FromArgb(82, iconColor.R, iconColor.G, iconColor.B);
            var borderBrush = new SolidColorBrush(borderColor);
            borderBrush.Freeze();
            var borderPen = new Pen(borderBrush, Math.Max(0.75, size * 0.028));
            borderPen.Freeze();

            dc.DrawRoundedRectangle(backgroundBrush, borderPen, tile, size * 0.30, size * 0.30);

            var highlightBrush = new SolidColorBrush(Color.FromArgb(95, 255, 255, 255));
            highlightBrush.Freeze();
            var highlight = new Rect(
                tile.X + size * 0.12,
                tile.Y + size * 0.10,
                tile.Width * 0.76,
                tile.Height * 0.17);
            dc.DrawRoundedRectangle(highlightBrush, null, highlight, size * 0.08, size * 0.08);
        }

        var inner = new Rect(
            tile.X + size * 0.21,
            tile.Y + size * 0.21,
            size * 0.58,
            size * 0.58);

        switch (kind)
        {
            case "All":
                DrawAllApps(dc, inner, iconBrush);
                break;
            case "Browser":
                DrawGlobe(dc, inner, iconBrush);
                break;
            case "Email":
                DrawEmail(dc, inner, iconBrush);
                break;
            case "Chat":
                DrawChat(dc, inner, iconBrush);
                break;
            case "Files":
                DrawFolder(dc, inner, iconBrush);
                break;
            case "Productivity":
                DrawProductivity(dc, inner, iconBrush);
                break;
            case "Development":
                DrawTextIcon(dc, "</>", "Consolas", inner, iconBrush, 0.93);
                break;
            case "System":
                DrawTextIcon(dc, "⚙", "Segoe UI Symbol", inner, iconBrush, 1.12);
                break;
            case "Media":
                DrawMedia(dc, inner, iconBrush);
                break;
            case "Design":
                DrawTextIcon(dc, "✦", "Segoe UI Symbol", inner, iconBrush, 1.05);
                break;
            case "Remote":
                DrawRemote(dc, inner, iconBrush);
                break;
            case "Clipboard":
                DrawClipboard(dc, inner, iconBrush);
                break;
            default:
                DrawOther(dc, inner, iconBrush);
                break;
        }
    }

    private static string Normalize(string? raw)
    {
        var value = (raw ?? "").Trim().ToLowerInvariant();

        if (value is "all" or "all apps") return "All";
        if (value.Contains("browser") || value == "web") return "Browser";
        if (value.Contains("email")) return "Email";
        if (value.Contains("chat") || value.Contains("meeting")) return "Chat";
        if (value is "file" or "files" || value.Contains("file") || value.Contains("document")) return "Files";
        if (value is "office" or "productivity" or "spreadsheet" or "presentation" or "notes") return "Productivity";
        if (value.Contains("develop")) return "Development";
        if (value.Contains("system")) return "System";
        if (value.Contains("media")) return "Media";
        if (value.Contains("design")) return "Design";
        if (value.Contains("remote")) return "Remote";
        if (value.Contains("clipboard")) return "Clipboard";

        return "Other";
    }

    private static Color Lighten(Color color, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            color.A,
            (byte)Math.Clamp(color.R + (255 - color.R) * amount, 0, 255),
            (byte)Math.Clamp(color.G + (255 - color.G) * amount, 0, 255),
            (byte)Math.Clamp(color.B + (255 - color.B) * amount, 0, 255));
    }

    private static (Color icon, Color background) ColorsFor(string kind) => kind switch
    {
        "All" => (Color.FromRgb(18, 92, 245), Color.FromRgb(211, 227, 255)),
        "Browser" => (Color.FromRgb(9, 187, 216), Color.FromRgb(210, 246, 251)),
        "Email" => (Color.FromRgb(255, 113, 15), Color.FromRgb(255, 228, 208)),
        "Chat" => (Color.FromRgb(195, 42, 230), Color.FromRgb(243, 216, 251)),
        "Files" => (Color.FromRgb(246, 187, 0), Color.FromRgb(255, 242, 194)),
        "Productivity" => (Color.FromRgb(10, 181, 194), Color.FromRgb(211, 246, 245)),
        "Development" => (Color.FromRgb(255, 105, 15), Color.FromRgb(255, 225, 205)),
        "System" => (Color.FromRgb(126, 139, 156), Color.FromRgb(226, 232, 239)),
        "Media" => (Color.FromRgb(235, 77, 85), Color.FromRgb(253, 235, 237)),
        "Design" => (Color.FromRgb(98, 87, 217), Color.FromRgb(239, 237, 253)),
        "Remote" => (Color.FromRgb(24, 175, 200), Color.FromRgb(230, 248, 251)),
        "Clipboard" => (Color.FromRgb(255, 138, 52), Color.FromRgb(255, 240, 227)),
        _ => (Color.FromRgb(126, 139, 156), Color.FromRgb(229, 234, 240))
    };

    private static Pen Pen(Brush brush, double size, double ratio = 0.095)
    {
        var pen = new Pen(brush, Math.Max(1.05, size * ratio))
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        pen.Freeze();
        return pen;
    }

    private static void DrawAllApps(DrawingContext dc, Rect r, Brush b)
    {
        var gap = r.Width * 0.15;
        var s = (r.Width - gap) / 2;
        var radius = s * 0.22;
        dc.DrawRoundedRectangle(b, null, new Rect(r.Left, r.Top, s, s), radius, radius);
        dc.DrawRoundedRectangle(b, null, new Rect(r.Left + s + gap, r.Top, s, s), radius, radius);
        dc.DrawRoundedRectangle(b, null, new Rect(r.Left, r.Top + s + gap, s, s), radius, radius);
        dc.DrawRoundedRectangle(b, null, new Rect(r.Left + s + gap, r.Top + s + gap, s, s), radius, radius);
    }

    private static void DrawGlobe(DrawingContext dc, Rect r, Brush b)
    {
        var p = Pen(b, r.Width, 0.09);
        var center = new Point(r.Left + r.Width / 2, r.Top + r.Height / 2);
        var radius = r.Width / 2;
        dc.DrawEllipse(null, p, center, radius, radius);
        dc.DrawLine(p, new Point(r.Left, center.Y), new Point(r.Right, center.Y));
        dc.DrawEllipse(null, p, center, radius * 0.48, radius);
    }

    private static void DrawEmail(DrawingContext dc, Rect r, Brush b)
    {
        var p = Pen(b, r.Width, 0.085);
        var box = new Rect(r.Left, r.Top + r.Height * 0.12, r.Width, r.Height * 0.76);
        dc.DrawRoundedRectangle(null, p, box, r.Width * 0.08, r.Width * 0.08);
        var mid = new Point(box.Left + box.Width / 2, box.Top + box.Height * 0.53);
        dc.DrawLine(p, new Point(box.Left, box.Top + box.Height * 0.08), mid);
        dc.DrawLine(p, new Point(box.Right, box.Top + box.Height * 0.08), mid);
    }

    private static void DrawChat(DrawingContext dc, Rect r, Brush b)
    {
        var back = new Rect(r.Left + r.Width * 0.30, r.Top + r.Height * 0.08, r.Width * 0.68, r.Height * 0.58);
        var front = new Rect(r.Left + r.Width * 0.02, r.Top + r.Height * 0.28, r.Width * 0.68, r.Height * 0.58);
        var radius = r.Width * 0.17;
        dc.DrawRoundedRectangle(b, null, back, radius, radius);
        dc.DrawRoundedRectangle(b, null, front, radius, radius);

        var tail1 = new StreamGeometry();
        using (var c = tail1.Open())
        {
            c.BeginFigure(new Point(back.Right - back.Width * 0.26, back.Bottom - 1), true, true);
            c.LineTo(new Point(back.Right - back.Width * 0.12, back.Bottom + r.Height * 0.18), true, false);
            c.LineTo(new Point(back.Right - back.Width * 0.43, back.Bottom - 1), true, false);
        }
        tail1.Freeze();
        dc.DrawGeometry(b, null, tail1);

        var tail2 = new StreamGeometry();
        using (var c = tail2.Open())
        {
            c.BeginFigure(new Point(front.Left + front.Width * 0.25, front.Bottom - 1), true, true);
            c.LineTo(new Point(front.Left + front.Width * 0.10, front.Bottom + r.Height * 0.16), true, false);
            c.LineTo(new Point(front.Left + front.Width * 0.42, front.Bottom - 1), true, false);
        }
        tail2.Freeze();
        dc.DrawGeometry(b, null, tail2);
    }

    private static void DrawFolder(DrawingContext dc, Rect r, Brush b)
    {
        var geometry = new StreamGeometry();
        using var c = geometry.Open();
        c.BeginFigure(new Point(r.Left, r.Top + r.Height * 0.24), true, true);
        c.LineTo(new Point(r.Left + r.Width * 0.34, r.Top + r.Height * 0.24), true, false);
        c.LineTo(new Point(r.Left + r.Width * 0.43, r.Top + r.Height * 0.10), true, false);
        c.LineTo(new Point(r.Left + r.Width * 0.68, r.Top + r.Height * 0.10), true, false);
        c.LineTo(new Point(r.Left + r.Width * 0.77, r.Top + r.Height * 0.24), true, false);
        c.LineTo(new Point(r.Right, r.Top + r.Height * 0.24), true, false);
        c.LineTo(new Point(r.Right, r.Bottom - r.Height * 0.08), true, false);
        c.LineTo(new Point(r.Left, r.Bottom - r.Height * 0.08), true, false);
        geometry.Freeze();
        dc.DrawGeometry(b, null, geometry);
    }

    private static void DrawProductivity(DrawingContext dc, Rect r, Brush b)
    {
        var gap = r.Width * 0.13;
        var barW = (r.Width - gap * 2) / 3;
        var heights = new[] { 0.47, 0.70, 0.96 };
        for (var i = 0; i < 3; i++)
        {
            var h = r.Height * heights[i];
            var x = r.Left + i * (barW + gap);
            var y = r.Bottom - h;
            dc.DrawRoundedRectangle(b, null, new Rect(x, y, barW, h), barW * 0.42, barW * 0.42);
        }
    }

    private void DrawTextIcon(DrawingContext dc, string text, string family, Rect r, Brush b, double scale)
    {
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var ft = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily(family), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
            r.Height * scale,
            b,
            dpi);

        var point = new Point(
            r.Left + (r.Width - ft.WidthIncludingTrailingWhitespace) / 2,
            r.Top + (r.Height - ft.Height) / 2 - r.Height * 0.02);
        dc.DrawText(ft, point);
    }

    private static void DrawMedia(DrawingContext dc, Rect r, Brush b)
    {
        var g = new StreamGeometry();
        using var c = g.Open();
        c.BeginFigure(new Point(r.Left + r.Width * 0.20, r.Top + r.Height * 0.08), true, true);
        c.LineTo(new Point(r.Right - r.Width * 0.10, r.Top + r.Height / 2), true, false);
        c.LineTo(new Point(r.Left + r.Width * 0.20, r.Bottom - r.Height * 0.08), true, false);
        g.Freeze();
        dc.DrawGeometry(b, null, g);
    }

    private static void DrawRemote(DrawingContext dc, Rect r, Brush b)
    {
        var p = Pen(b, r.Width, 0.09);
        var screen = new Rect(r.Left, r.Top + r.Height * 0.05, r.Width, r.Height * 0.66);
        dc.DrawRoundedRectangle(null, p, screen, r.Width * 0.08, r.Width * 0.08);
        var cx = r.Left + r.Width / 2;
        dc.DrawLine(p, new Point(cx, screen.Bottom), new Point(cx, r.Bottom - r.Height * 0.08));
        dc.DrawLine(p, new Point(r.Left + r.Width * 0.32, r.Bottom - r.Height * 0.08), new Point(r.Right - r.Width * 0.32, r.Bottom - r.Height * 0.08));
    }

    private static void DrawClipboard(DrawingContext dc, Rect r, Brush b)
    {
        var p = Pen(b, r.Width, 0.08);
        var box = new Rect(r.Left + r.Width * 0.12, r.Top + r.Height * 0.14, r.Width * 0.76, r.Height * 0.80);
        dc.DrawRoundedRectangle(null, p, box, r.Width * 0.08, r.Width * 0.08);
        var clip = new Rect(r.Left + r.Width * 0.31, r.Top, r.Width * 0.38, r.Height * 0.28);
        dc.DrawRoundedRectangle(b, null, clip, r.Width * 0.08, r.Width * 0.08);
    }

    private static void DrawOther(DrawingContext dc, Rect r, Brush b)
    {
        var radius = r.Width * 0.095;
        var cy = r.Top + r.Height / 2;
        dc.DrawEllipse(b, null, new Point(r.Left + r.Width * 0.22, cy), radius, radius);
        dc.DrawEllipse(b, null, new Point(r.Left + r.Width * 0.50, cy), radius, radius);
        dc.DrawEllipse(b, null, new Point(r.Left + r.Width * 0.78, cy), radius, radius);
    }
}
