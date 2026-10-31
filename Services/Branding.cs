using System;
using System.Windows;
using System.Windows.Media;

namespace MemoryLane.Windows;

public static class Branding
{
    private static readonly ImageSource _icon = BuildIcon();

    public static ImageSource IconSource => _icon;

    public static ImageSource? LoadImage() => _icon;

    private static ImageSource BuildIcon()
    {
        var group = new DrawingGroup();

        using (var dc = group.Open())
        {
            var background = new LinearGradientBrush(
                Color.FromRgb(26, 82, 166),
                Color.FromRgb(65, 55, 158),
                new Point(0, 0),
                new Point(1, 1));
            background.Freeze();

            dc.DrawRoundedRectangle(background,
                new Pen(new SolidColorBrush(Color.FromArgb(130, 145, 199, 255)), 8),
                new Rect(42, 42, 940, 940), 190, 190);

            var left = new LinearGradientBrush(
                Color.FromRgb(35, 190, 245),
                Color.FromRgb(61, 130, 236),
                new Point(0, 0),
                new Point(1, 1));
            var right = new LinearGradientBrush(
                Color.FromRgb(255, 111, 146),
                Color.FromRgb(245, 155, 70),
                new Point(0, 0),
                new Point(1, 1));

            DrawBrainLobes(dc, left, true);
            DrawBrainLobes(dc, right, false);

            var white = new SolidColorBrush(Color.FromArgb(225, 255, 255, 255));
            var cyan = new SolidColorBrush(Color.FromArgb(220, 194, 246, 255));
            var warm = new SolidColorBrush(Color.FromArgb(220, 255, 222, 177));
            var detailPenLeft = new Pen(cyan, 12) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            var detailPenRight = new Pen(warm, 12) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            var seamPen = new Pen(white, 15) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

            dc.DrawLine(seamPen, new Point(512, 258), new Point(500, 492));
            dc.DrawLine(detailPenLeft, new Point(270, 350), new Point(380, 330));
            dc.DrawLine(detailPenLeft, new Point(320, 465), new Point(420, 420));
            dc.DrawLine(detailPenLeft, new Point(330, 575), new Point(420, 530));
            dc.DrawLine(detailPenRight, new Point(754, 350), new Point(644, 330));
            dc.DrawLine(detailPenRight, new Point(704, 465), new Point(604, 420));
            dc.DrawLine(detailPenRight, new Point(694, 575), new Point(604, 530));

            var road = new StreamGeometry();
            using (var ctx = road.Open())
            {
                ctx.BeginFigure(new Point(430, 520), true, true);
                ctx.BezierTo(new Point(520, 520), new Point(650, 545), new Point(700, 650), true, false);
                ctx.BezierTo(new Point(745, 760), new Point(625, 885), new Point(540, 955), true, false);
                ctx.LineTo(new Point(390, 955), true, false);
                ctx.BezierTo(new Point(485, 850), new Point(560, 730), new Point(540, 650), true, false);
                ctx.BezierTo(new Point(520, 580), new Point(470, 545), new Point(430, 520), true, false);
            }
            road.Freeze();
            dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(74, 154, 235)), null, road);

            var centerLine = new StreamGeometry();
            using (var ctx = centerLine.Open())
            {
                ctx.BeginFigure(new Point(500, 550), false, false);
                ctx.BezierTo(new Point(585, 590), new Point(600, 675), new Point(565, 735), true, false);
                ctx.BezierTo(new Point(535, 800), new Point(505, 855), new Point(480, 910), true, false);
            }
            centerLine.Freeze();
            var linePen = new Pen(new SolidColorBrush(Color.FromArgb(235, 250, 253, 255)), 12)
            { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            dc.DrawGeometry(null, linePen, centerLine);
        }

        group.Freeze();
        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }

    private static void DrawBrainLobes(DrawingContext dc, Brush brush, bool left)
    {
        var outline = new Pen(new SolidColorBrush(left
            ? Color.FromArgb(220, 139, 229, 255)
            : Color.FromArgb(220, 255, 194, 125)), 8);

        var centers = left
            ? new[]
            {
                new Point(285, 350), new Point(380, 285), new Point(455, 365),
                new Point(310, 455), new Point(410, 485), new Point(335, 565)
            }
            : new[]
            {
                new Point(739, 350), new Point(644, 285), new Point(569, 365),
                new Point(714, 455), new Point(614, 485), new Point(689, 565)
            };

        var radii = new[] { 105.0, 100.0, 92.0, 102.0, 88.0, 96.0 };
        for (var i = 0; i < centers.Length; i++)
            dc.DrawEllipse(brush, outline, centers[i], radii[i], radii[i] * 0.92);
    }
}