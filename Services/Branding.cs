using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MemoryLane.Windows;

public static class Branding
{
    private static readonly ImageSource _icon = BuildIcon();

    public static ImageSource IconSource => _icon;
    public static ImageSource? LoadImage() => _icon;

    public static System.Drawing.Icon CreateTrayIcon()
    {
        const int size = 64;
        var visual = new DrawingVisual();

        using (var dc = visual.RenderOpen())
            dc.DrawImage(_icon, new Rect(0, 0, size, size));

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        stream.Position = 0;

        using var sourceBitmap = new System.Drawing.Bitmap(stream);
        var hIcon = sourceBitmap.GetHicon();
        try
        {
            using var icon = System.Drawing.Icon.FromHandle(hIcon);
            return (System.Drawing.Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    private static ImageSource BuildIcon()
    {
        var drawing = new DrawingGroup();

        using (var dc = drawing.Open())
        {
            var background = new LinearGradientBrush(
                Color.FromRgb(13, 77, 149),
                Color.FromRgb(101, 88, 217),
                new Point(0, 0),
                new Point(1, 1));
            background.Freeze();

            dc.DrawRoundedRectangle(
                background,
                new Pen(new SolidColorBrush(Color.FromArgb(90, 183, 223, 255)), 8),
                new Rect(42, 42, 940, 940),
                220,
                220);

            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(38, 111, 227, 255)), null,
                new Point(300, 300), 190, 190);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(32, 255, 138, 200)), null,
                new Point(730, 315), 180, 180);

            var nodeA = new LinearGradientBrush(
                Color.FromRgb(99, 230, 255),
                Color.FromRgb(90, 177, 255),
                new Point(0, 0),
                new Point(1, 1));
            var nodeB = new LinearGradientBrush(
                Color.FromRgb(255, 139, 203),
                Color.FromRgb(255, 177, 93),
                new Point(0, 0),
                new Point(1, 1));

            dc.DrawEllipse(nodeA, null, new Point(310, 275), 72, 72);
            dc.DrawEllipse(nodeB, null, new Point(718, 300), 62, 62);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(99, 215, 255)), null, new Point(275, 510), 44, 44);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 191, 102)), null, new Point(750, 515), 46, 46);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(142, 235, 217)), null, new Point(395, 670), 34, 34);

            var connector = new Pen(new SolidColorBrush(Color.FromArgb(150, 221, 247, 255)), 18)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            DrawCurve(dc, connector, new Point(366, 292), new Point(470, 218), new Point(565, 220), new Point(662, 292));
            DrawCurve(dc, connector, new Point(286, 340), new Point(232, 393), new Point(229, 452), new Point(262, 492));
            DrawCurve(dc, connector, new Point(737, 356), new Point(790, 400), new Point(792, 460), new Point(760, 497));
            DrawCurve(dc, connector, new Point(300, 550), new Point(330, 605), new Point(360, 635), new Point(392, 661));

            var roadGeometry = new StreamGeometry();
            using (var ctx = roadGeometry.Open())
            {
                ctx.BeginFigure(new Point(350, 255), false, false);
                ctx.BezierTo(new Point(470, 326), new Point(588, 364), new Point(620, 455), true, false);
                ctx.BezierTo(new Point(657, 560), new Point(574, 626), new Point(512, 684), true, false);
                ctx.BezierTo(new Point(451, 741), new Point(432, 824), new Point(461, 900), true, false);
            }
            roadGeometry.Freeze();

            var shadowPen = new Pen(new SolidColorBrush(Color.FromArgb(105, 10, 60, 124)), 126)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            dc.DrawGeometry(null, shadowPen, roadGeometry);

            var outerRoad = new LinearGradientBrush(
                Color.FromRgb(168, 238, 255),
                Color.FromRgb(255, 255, 255),
                new Point(0, 0),
                new Point(1, 1));
            var outerPen = new Pen(outerRoad, 88)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            dc.DrawGeometry(null, outerPen, roadGeometry);

            var innerPen = new Pen(new SolidColorBrush(Color.FromRgb(52, 127, 216)), 48)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            dc.DrawGeometry(null, innerPen, roadGeometry);

            var highlight = new StreamGeometry();
            using (var ctx = highlight.Open())
            {
                ctx.BeginFigure(new Point(359, 264), false, false);
                ctx.BezierTo(new Point(468, 330), new Point(560, 366), new Point(583, 445), true, false);
                ctx.BezierTo(new Point(611, 540), new Point(540, 595), new Point(482, 648), true, false);
                ctx.BezierTo(new Point(426, 700), new Point(409, 777), new Point(432, 848), true, false);
            }
            highlight.Freeze();

            var highlightPen = new Pen(new SolidColorBrush(Color.FromArgb(225, 255, 255, 255)), 13)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            dc.DrawGeometry(null, highlightPen, highlight);

            var spark = new StreamGeometry();
            using (var ctx = spark.Open())
            {
                ctx.BeginFigure(new Point(350, 176), true, true);
                ctx.LineTo(new Point(366, 232), true, false);
                ctx.LineTo(new Point(422, 248), true, false);
                ctx.LineTo(new Point(366, 264), true, false);
                ctx.LineTo(new Point(350, 320), true, false);
                ctx.LineTo(new Point(334, 264), true, false);
                ctx.LineTo(new Point(278, 248), true, false);
                ctx.LineTo(new Point(334, 232), true, false);
            }
            spark.Freeze();
            dc.DrawGeometry(Brushes.White, null, spark);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(123, 223, 255)), null, new Point(350, 248), 13, 13);
        }

        drawing.Freeze();
        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }

    private static void DrawCurve(DrawingContext dc, Pen pen, Point start, Point c1, Point c2, Point end)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(start, false, false);
            ctx.BezierTo(c1, c2, end, true, false);
        }
        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
