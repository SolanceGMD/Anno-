Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class AnnoPlusNative {
  [DllImport("user32.dll", SetLastError=true)] public static extern bool DestroyIcon(IntPtr hIcon);
}
"@
Add-Type -AssemblyName System.Drawing

$bitmap = [System.Drawing.Bitmap]::new(64, 64, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$path = [System.Drawing.Drawing2D.GraphicsPath]::new()
$brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 71, 120, 232))
$font = [System.Drawing.Font]::new("Segoe UI", 23, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$textBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$graphics.Clear([System.Drawing.Color]::Transparent)

# Rounded-square app tile.
$path.AddArc(2, 2, 16, 16, 180, 90)
$path.AddArc(46, 2, 16, 16, 270, 90)
$path.AddArc(46, 46, 16, 16, 0, 90)
$path.AddArc(2, 46, 16, 16, 90, 90)
$path.CloseFigure()
$graphics.FillPath($brush, $path)
$graphics.DrawString("a+", $font, $textBrush, 7, 14)

$hIcon = $bitmap.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)
$outPath = Join-Path $PSScriptRoot "..\assets\anno-plus.ico"
try {
    $stream = [System.IO.File]::Create($outPath)
    try { $icon.Save($stream) } finally { $stream.Dispose() }
} finally {
    [AnnoPlusNative]::DestroyIcon($hIcon) | Out-Null
    $icon.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
    $brush.Dispose()
    $font.Dispose()
    $textBrush.Dispose()
    $path.Dispose()
}
Write-Host "Generated $outPath"
