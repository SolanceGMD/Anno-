# anno+

A Windows desktop annotation app for students and educators. This starter release lets you import an image, mark it with a pen, highlighter, arrow, rectangle, or ellipse, undo strokes, clear annotations, and export the result as PNG.

## Downloads

Every push to `main` builds two Windows x64 downloads:
- **`anno+-setup.exe`** — Windows installer, with Start Menu and optional desktop shortcuts.
- **`anno+-portable.exe`** — portable single-file executable; no installation step.

Both are self-contained .NET 8 Windows builds, so users do not need to install .NET separately. The app targets Windows 10/11 x64.

## Build

Open `AnnoPlus.sln` in Visual Studio 2022 with the .NET desktop development workload, or run:

```powershell
dotnet publish src/AnnoPlus/AnnoPlus.csproj -c Release -r win-x64 --self-contained true
```

GitHub Actions builds the portable executable and installer automatically. Open the repository's **Actions** tab, select **Build Windows app and installer**, and download the `anno-plus-windows-x64` artifact after a successful run. Create a version tag such as `v0.1.0` to publish the same downloads as a GitHub Release.

## Current scope

This is an early MVP focused on image annotation and PNG export. PDF/multi-page support, editable/movable existing marks, saved project files, and cloud collaboration are not included yet.
