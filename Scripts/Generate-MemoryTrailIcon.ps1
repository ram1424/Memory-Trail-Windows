param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$assets = Join-Path $RepositoryRoot "Assets"
New-Item -ItemType Directory -Path $assets -Force | Out-Null

function New-RoundedPath {
    param([System.Drawing.RectangleF]$Rect, [float]$Radius)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $Radius * 2
    $arc = New-Object System.Drawing.RectangleF($Rect.X, $Rect.Y, $diameter, $diameter)
    $path.AddArc($arc, 180, 90)
    $arc.X = $Rect.Right - $diameter
    $path.AddArc($arc, 270, 90)
    $arc.Y = $Rect.Bottom - $diameter
    $path.AddArc($arc, 0, 90)
    $arc.X = $Rect.Left
    $path.AddArc($arc, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-GradientBrush {
    param(
        [System.Drawing.RectangleF]$Rect,
        [System.Drawing.Color]$Start,
        [System.Drawing.Color]$End,
        [float]$Angle = 45
    )
    return New-Object System.Drawing.Drawing2D.LinearGradientBrush($Rect, $Start, $End, $Angle)
}

function Draw-ActivityCard {
    param(
        [System.Drawing.Graphics]$Graphics,
        [float]$X,
        [float]$Y,
        [float]$Width,
        [float]$Height,
        [System.Drawing.Color]$Accent,
        [string]$Glyph
    )

    $shadowRect = New-Object System.Drawing.RectangleF($X + 7, $Y + 9, $Width, $Height)
    $shadowPath = New-RoundedPath $shadowRect 24
    $shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(48, 18, 45, 91))
    $Graphics.FillPath($shadowBrush, $shadowPath)

    $rect = New-Object System.Drawing.RectangleF($X, $Y, $Width, $Height)
    $path = New-RoundedPath $rect 24
    $brush = New-GradientBrush $rect ([System.Drawing.Color]::FromArgb(244, 248, 255)) ([System.Drawing.Color]::FromArgb(180, 214, 245)) 10
    $Graphics.FillPath($brush, $path)
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(190, 255, 255, 255), 3)
    $Graphics.DrawPath($pen, $path)

    $iconRect = New-Object System.Drawing.RectangleF($X + 20, $Y + 17, $Height - 34, $Height - 34)
    $iconPath = New-RoundedPath $iconRect 15
    $iconBrush = New-Object System.Drawing.SolidBrush($Accent)
    $Graphics.FillPath($iconBrush, $iconPath)

    $font = New-Object System.Drawing.Font("Segoe UI Symbol", [Math]::Max(20, $Height * 0.34), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $Graphics.DrawString($Glyph, $font, $white, $iconRect, $format)

    $lineBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(110, 74, 105, 151))
    $lineX = $iconRect.Right + 18
    $lineY = $Y + ($Height * 0.38)
    $Graphics.FillRectangle($lineBrush, $lineX, $lineY, $Width - ($lineX - $X) - 26, 10)
    $Graphics.FillRectangle($lineBrush, $lineX, $lineY + 24, ($Width - ($lineX - $X) - 26) * 0.72, 10)

    foreach ($item in @($shadowBrush,$shadowPath,$brush,$pen,$path,$iconBrush,$iconPath,$font,$format,$white,$lineBrush)) {
        if ($item -is [System.IDisposable]) { $item.Dispose() }
    }
}

$size = 1024
$bitmap = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$graphics.Clear([System.Drawing.Color]::Transparent)

$outerRect = New-Object System.Drawing.RectangleF(18, 18, 988, 988)
$outerPath = New-RoundedPath $outerRect 205
$outerBrush = New-GradientBrush $outerRect ([System.Drawing.Color]::FromArgb(17, 80, 185)) ([System.Drawing.Color]::FromArgb(83, 64, 186)) 35
$graphics.FillPath($outerBrush, $outerPath)

# Sunset and layered hills.
$sunGlow = New-Object System.Drawing.Drawing2D.GraphicsPath
$sunGlow.AddEllipse(680, 95, 290, 290)
$sunBrush = New-Object System.Drawing.Drawing2D.PathGradientBrush($sunGlow)
$sunBrush.CenterColor = [System.Drawing.Color]::FromArgb(255, 255, 246, 186)
$sunBrush.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 255, 129, 86))
$graphics.FillPath($sunBrush, $sunGlow)
$graphics.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 245, 177))), 758, 174, 142, 142)

$hillBrush1 = New-GradientBrush (New-Object System.Drawing.RectangleF(0, 250, 1024, 770)) ([System.Drawing.Color]::FromArgb(34, 75, 164)) ([System.Drawing.Color]::FromArgb(34, 158, 181)) 90
$hill1 = New-Object System.Drawing.Drawing2D.GraphicsPath
$hill1.AddBezier(-80, 560, 210, 360, 420, 550, 680, 390)
$hill1.AddBezier(680, 390, 860, 290, 1020, 410, 1100, 330)
$hill1.AddLine(1100, 1050); $hill1.AddLine(-80,1050); $hill1.CloseFigure()
$graphics.FillPath($hillBrush1, $hill1)

$hillBrush2 = New-GradientBrush (New-Object System.Drawing.RectangleF(0, 450, 1024, 580)) ([System.Drawing.Color]::FromArgb(32, 54, 135)) ([System.Drawing.Color]::FromArgb(75, 48, 145)) 15
$hill2 = New-Object System.Drawing.Drawing2D.GraphicsPath
$hill2.AddBezier(-70, 750, 180, 540, 410, 710, 670, 565)
$hill2.AddBezier(670, 565, 820, 470, 980, 590, 1100, 510)
$hill2.AddLine(1100,1050); $hill2.AddLine(-70,1050); $hill2.CloseFigure()
$graphics.FillPath($hillBrush2, $hill2)

# Activity cards.
Draw-ActivityCard $graphics 102 120 490 142 ([System.Drawing.Color]::FromArgb(23, 122, 232)) "◎"
Draw-ActivityCard $graphics 72 265 495 142 ([System.Drawing.Color]::FromArgb(37, 111, 220)) "✉"
Draw-ActivityCard $graphics 48 410 500 142 ([System.Drawing.Color]::FromArgb(34, 177, 101)) "●"
Draw-ActivityCard $graphics 44 555 490 142 ([System.Drawing.Color]::FromArgb(245, 158, 11)) "▰"
Draw-ActivityCard $graphics 82 700 490 142 ([System.Drawing.Color]::FromArgb(124, 78, 214)) "▦"

# Road / trail.
$road = New-Object System.Drawing.Drawing2D.GraphicsPath
$road.StartFigure()
$road.AddBezier(85, 985, 285, 800, 680, 920, 740, 670)
$road.AddBezier(740, 670, 785, 485, 655, 440, 835, 328)
$road.AddBezier(835, 328, 900, 286, 900, 264, 890, 250)
$roadPenGlow = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(110, 87, 233, 255), 182)
$roadPenGlow.StartCap = $roadPenGlow.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$roadPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 35, 118, 222), 142)
$roadPen.StartCap = $roadPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$roadEdge = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 255, 196, 86), 154)
$roadEdge.StartCap = $roadEdge.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$graphics.DrawPath($roadPenGlow, $road)
$graphics.DrawPath($roadEdge, $road)
$graphics.DrawPath($roadPen, $road)
$dashPen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 13)
$dashPen.DashPattern = [float[]]@(3.0, 2.6)
$dashPen.StartCap = $dashPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$graphics.DrawPath($dashPen, $road)

# Clock / recall symbol.
$clockOuter = New-Object System.Drawing.RectangleF(566, 525, 338, 338)
$clockBrush = New-GradientBrush $clockOuter ([System.Drawing.Color]::FromArgb(45, 235, 255)) ([System.Drawing.Color]::FromArgb(120, 92, 232)) 45
$graphics.FillEllipse($clockBrush, $clockOuter)
$graphics.FillEllipse((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 14, 65, 139))), 604, 563, 262, 262)
$whitePen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 20)
$whitePen.StartCap = $whitePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$graphics.DrawLine($whitePen, 735, 694, 735, 618)
$graphics.DrawLine($whitePen, 735, 694, 800, 750)
$arrowPen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 23)
$arrowPen.StartCap = $arrowPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$graphics.DrawArc($arrowPen, 585, 544, 300, 300, 210, 240)
$graphics.DrawLine($arrowPen, 596, 654, 566, 710)
$graphics.DrawLine($arrowPen, 596, 654, 648, 675)

# Final rim.
$rimPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(205, 142, 231, 255), 7)
$graphics.DrawPath($rimPen, $outerPath)

$masterPath = Join-Path $assets "MemoryTrailIconMaster.png"
$smallPath = Join-Path $assets "MemoryTrailIcon.png"
$icoPath = Join-Path $assets "MemoryTrailApp.ico"
$bitmap.Save($masterPath, [System.Drawing.Imaging.ImageFormat]::Png)
$small = New-Object System.Drawing.Bitmap($bitmap, 256, 256)
$small.Save($smallPath, [System.Drawing.Imaging.ImageFormat]::Png)

# ICO supports PNG payloads. A single high-quality 256px entry scales cleanly in Windows shell surfaces.
$png = [System.IO.File]::ReadAllBytes($smallPath)
$stream = [System.IO.File]::Create($icoPath)
$writer = New-Object System.IO.BinaryWriter($stream)
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]1)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]32)
$writer.Write([UInt32]$png.Length)
$writer.Write([UInt32]22)
$writer.Write($png)
$writer.Dispose()
$stream.Dispose()

foreach ($item in @($graphics,$bitmap,$small,$outerPath,$outerBrush,$sunGlow,$sunBrush,$hillBrush1,$hillBrush2,$hill1,$hill2,$road,$roadPenGlow,$roadPen,$roadEdge,$dashPen,$clockBrush,$whitePen,$arrowPen,$rimPen)) {
    if ($item -is [System.IDisposable]) { $item.Dispose() }
}

Write-Host "Generated Memory Trail icon assets in $assets"
