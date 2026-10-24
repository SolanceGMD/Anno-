Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class AnnoPlusNative {
  [DllImport("user32.dll", SetLastError=true)] public static extern bool DestroyIcon(IntPtr hIcon);
}
"@
Add-Type -AssemblyName System.Drawing
$size = 64
$bitmap = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$graphics.Clear([System.Drawing.Color]::Transparent)
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddArc(2, 2, 60, 60, 180, 90)
$path.AddArc(2, 2, 60, 60, 270, 90)
$path.AddArc(2, 2, 60, 60, 0, 90)
$path.AddArc(2, 2, 60, 60, 90, 90)
$path.CloseFigure()
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 71, 120, 232))
$graphics.FillPath($brush, $path)
$font = New-Object System.Drawing.Font("Segoe UI", 23, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$graphics.DrawString("a+", $font, $textBrush, 7, 14)
$hIcon = $bitmap.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)
$stream = [System.IO.File]::Create((Join-Path $PSScriptRoot "..\assets\anno-plus.ico"))
try { $icon.Save($stream) } finally { $stream.Dispose(); $icon.Dispose(); $graphics.Dispose(); $bitmap.Dispose(); $brush.Dispose(); $font.Dispose(); $textBrush.Dispose(); $path.Dispose(); [AnnoPlusNative]::DestroyIcon($hIcon) }
