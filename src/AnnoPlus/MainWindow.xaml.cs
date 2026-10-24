using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Windows.Shapes;

namespace AnnoPlus;

public partial class MainWindow : Window
{
    private string _tool = "Select";
    private Brush _ink = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4778E8"));
    private bool _drawing, _moving;
    private Point _start, _moveOffset;
    private UIElement? _preview, _selected, _clipboardElement;
    private readonly Stack<UIElement> _undo = new();
    private string? _projectPath;

    public MainWindow() => InitializeComponent();

    private void NewWorkspace_Click(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas.Children.Count > 0 && MessageBox.Show("Start a new workspace? Unsaved work will be lost.", "anno+", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        DrawingCanvas.Children.Clear(); _undo.Clear(); _selected = null; _projectPath = null;
        DrawingCanvas.Width = 1600; DrawingCanvas.Height = 1100; DocumentLabel.Text = "Untitled workspace";
        SelectionLabel.Text = "Nothing selected"; StatusLabel.Text = "New blank workspace";
    }

    private void OpenImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Add images to your workspace", Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|All files|*.*", Multiselect = true };
        if (dialog.ShowDialog() != true) return;
        foreach (var path in dialog.FileNames) try { AddImage(path, 80 + (DrawingCanvas.Children.Count % 8) * 28, 70 + (DrawingCanvas.Children.Count % 8) * 28); }
        catch (Exception ex) { MessageBox.Show($"Could not open {System.IO.Path.GetFileName(path)}.\n{ex.Message}", "anno+", MessageBoxButton.OK, MessageBoxImage.Error); }
        DocumentLabel.Text = dialog.FileNames.Length == 1 ? System.IO.Path.GetFileName(dialog.FileNames[0]) : $"{dialog.FileNames.Length} images in workspace";
        StatusLabel.Text = $"Added {dialog.FileNames.Length} image(s). Select and drag to arrange them.";
    }

    private void AddImage(string path, double left, double top)
    {
        var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.UriSource = new Uri(path); bitmap.EndInit(); bitmap.Freeze();
        var maxW = 650d; var scale = Math.Min(1, maxW / bitmap.PixelWidth);
        var image = new Image { Source = bitmap, Width = bitmap.PixelWidth * scale, Height = bitmap.PixelHeight * scale, Stretch = Stretch.Uniform, SnapsToDevicePixels = true, Cursor = Cursors.SizeAll };
        Canvas.SetLeft(image, left); Canvas.SetTop(image, top); DrawingCanvas.Children.Add(image); _undo.Push(image); SelectElement(image);
    }

    private void StockImages_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://unsplash.com/s/photos/education") { UseShellExecute = true }); }
        catch { MessageBox.Show("Open https://unsplash.com/s/photos/education in your browser to find free stock imagery.", "anno+"); }
        StatusLabel.Text = "Stock image browser opened. Download an image, then drag it onto the workspace.";
    }

    private void AddText_Click(object sender, RoutedEventArgs e) { _tool = "Text"; TextTool.IsChecked = true; StatusLabel.Text = "Click anywhere on the workspace to place text."; }

    private void Tool_Checked(object sender, RoutedEventArgs e)
    {
        if (SelectTool?.IsChecked == true) _tool = "Select";
        else if (PenTool?.IsChecked == true) _tool = "Pen";
        else if (HighlightTool?.IsChecked == true) _tool = "Highlight";
        else if (ArrowTool?.IsChecked == true) _tool = "Arrow";
        else if (RectTool?.IsChecked == true) _tool = "Rectangle";
        else if (EllipseTool?.IsChecked == true) _tool = "Ellipse";
        else if (TextTool?.IsChecked == true) _tool = "Text";
        if (DrawingCanvas != null) DrawingCanvas.Cursor = _tool == "Select" ? Cursors.Arrow : _tool == "Text" ? Cursors.IBeam : Cursors.Cross;
    }

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string value) { _ink = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value)); StatusLabel.Text = $"Ink colour: {value}"; }
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var p = e.GetPosition(DrawingCanvas);
        if (_tool == "Select")
        {
            var element = FindCanvasChild(e.OriginalSource as DependencyObject);
            if (element is not null)
            {
                SelectElement(element); _moving = true; _start = p;
                _moveOffset = new Point(p.X - Canvas.GetLeft(element), p.Y - Canvas.GetTop(element));
                if (double.IsNaN(_moveOffset.X)) _moveOffset.X = p.X;
                if (double.IsNaN(_moveOffset.Y)) _moveOffset.Y = p.Y;
                DrawingCanvas.CaptureMouse(); e.Handled = true;
            }
            else { SelectElement(null); }
            return;
        }
        if (_tool == "Text")
        {
            var text = new TextBox { Text = "Double-click to edit this note", FontSize = 24, FontFamily = new FontFamily("Segoe UI"), Foreground = _ink, Background = Brushes.Transparent, BorderThickness = new Thickness(0), MinWidth = 180, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Padding = new Thickness(4), Cursor = Cursors.IBeam };
            Canvas.SetLeft(text, p.X); Canvas.SetTop(text, p.Y); DrawingCanvas.Children.Add(text); _undo.Push(text); SelectElement(text); text.Focus(); text.SelectAll(); _tool = "Select"; SelectTool.IsChecked = true; e.Handled = true; return;
        }
        _drawing = true; _start = p; DrawingCanvas.CaptureMouse();
        if (_tool is "Pen" or "Highlight")
        {
            var c = ((SolidColorBrush)_ink).Color;
            var stroke = new Polyline { Stroke = _tool == "Highlight" ? new SolidColorBrush(Color.FromArgb(95, c.R, c.G, c.B)) : _ink, StrokeThickness = _tool == "Highlight" ? Math.Max(15, WidthSlider.Value * 4) : WidthSlider.Value, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round, Points = new PointCollection { p }, IsHitTestVisible = false };
            _preview = stroke; DrawingCanvas.Children.Add(stroke);
        }
        else
        {
            Shape shape = _tool == "Ellipse" ? new Ellipse() : _tool == "Rectangle" ? new Rectangle() : new Line();
            shape.Stroke = _ink; shape.StrokeThickness = WidthSlider.Value; shape.IsHitTestVisible = false;
            if (shape is Line line) { line.X1 = p.X; line.Y1 = p.Y; line.X2 = p.X; line.Y2 = p.Y; line.StrokeEndLineCap = PenLineCap.Triangle; }
            else shape.Fill = Brushes.Transparent;
            _preview = shape; DrawingCanvas.Children.Add(shape);
        }
        e.Handled = true;
    }

    private UIElement? FindCanvasChild(DependencyObject? current)
    {
        while (current != null && !ReferenceEquals(current, DrawingCanvas))
        {
            if (current is UIElement element && ReferenceEquals(VisualTreeHelper.GetParent(current), DrawingCanvas)) return element;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        var p = e.GetPosition(DrawingCanvas);
        if (_moving && _selected != null)
        {
            Canvas.SetLeft(_selected, Math.Max(0, p.X - _moveOffset.X)); Canvas.SetTop(_selected, Math.Max(0, p.Y - _moveOffset.Y)); return;
        }
        if (!_drawing || _preview is null) return;
        if (_preview is Polyline polyline) polyline.Points.Add(p);
        else if (_preview is Line line) { line.X2 = p.X; line.Y2 = p.Y; }
        else if (_preview is Shape shape) { Canvas.SetLeft(shape, Math.Min(_start.X, p.X)); Canvas.SetTop(shape, Math.Min(_start.Y, p.Y)); shape.Width = Math.Abs(p.X - _start.X); shape.Height = Math.Abs(p.Y - _start.Y); }
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_moving) { _moving = false; DrawingCanvas.ReleaseMouseCapture(); if (_selected != null) _undo.Push(_selected); return; }
        if (!_drawing) return;
        _drawing = false; DrawingCanvas.ReleaseMouseCapture(); if (_preview != null) { _undo.Push(_preview); SelectElement(_preview); } _preview = null;
    }

    private void SelectElement(UIElement? element)
    {
        _selected = element;
        SelectionLabel.Text = element switch { null => "Nothing selected", Image => "Image · drag to move", TextBox => "Text note · click to edit, drag to move", Polyline => "Freehand annotation", Shape => "Shape / annotation", _ => element.GetType().Name };
        StatusLabel.Text = element == null ? "Ready" : "Object selected · Ctrl+C to copy, Delete to remove";
    }

    private UIElement? CloneElement(UIElement? element)
    {
        if (element is null) return null;
        try
        {
            var xaml = XamlWriter.Save(element);
            if (XamlReader.Parse(xaml) is not UIElement clone) return null;
            Canvas.SetLeft(clone, Canvas.GetLeft(element) + 24); Canvas.SetTop(clone, Canvas.GetTop(element) + 24);
            if (clone is FrameworkElement f) f.IsHitTestVisible = true;
            return clone;
        }
        catch { return null; }
    }

    private void Copy_Click(object sender, RoutedEventArgs e) => CopySelected();
    private void Cut_Click(object sender, RoutedEventArgs e) { CopySelected(); DeleteSelected(); }
    private void Paste_Click(object sender, RoutedEventArgs e) => PasteSelected();
    private void Duplicate_Click(object sender, RoutedEventArgs e) { CopySelected(); PasteSelected(); }
    private void CopySelected() { _clipboardElement = CloneElement(_selected); StatusLabel.Text = _clipboardElement == null ? "Select an object to copy" : "Copied object to anno+ clipboard"; }
    private void PasteSelected()
    {
        if (_clipboardElement != null)
        {
            var clone = CloneElement(_clipboardElement); if (clone == null) return;
            DrawingCanvas.Children.Add(clone); _undo.Push(clone); SelectElement(clone);
        }
        else if (Clipboard.ContainsImage())
        {
            var img = Clipboard.GetImage(); if (img != null) { var image = new Image { Source = img, Width = img.Width, Height = img.Height, Stretch = Stretch.Uniform }; Canvas.SetLeft(image, 80); Canvas.SetTop(image, 80); DrawingCanvas.Children.Add(image); _undo.Push(image); SelectElement(image); }
        }
        else if (Clipboard.ContainsFileDropList())
        {
            foreach (string file in Clipboard.GetFileDropList()) if (File.Exists(file) && IsImage(file)) AddImage(file, 90, 90);
        }
    }
    private void Delete_Click(object sender, RoutedEventArgs e) => DeleteSelected();
    private void DeleteSelected() { if (_selected != null) { DrawingCanvas.Children.Remove(_selected); _selected = null; SelectElement(null); } }
    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (_undo.Count == 0) return;
        var item = _undo.Pop(); DrawingCanvas.Children.Remove(item); if (ReferenceEquals(item, _selected)) SelectElement(null);
        StatusLabel.Text = "Last action undone";
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Clear every object from this workspace?", "anno+", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        DrawingCanvas.Children.Clear(); _undo.Clear(); SelectElement(null); StatusLabel.Text = "Workspace cleared";
    }

    private void ZoomSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (CanvasScale == null || ZoomLabel == null) return;
        var s = ZoomSlider.Value / 100d; CanvasScale.ScaleX = s; CanvasScale.ScaleY = s; ZoomLabel.Text = $"{ZoomSlider.Value:0}%";
    }
    private void ZoomIn_Click(object sender, RoutedEventArgs e) => ZoomSlider.Value = Math.Min(200, ZoomSlider.Value + 10);
    private void ZoomOut_Click(object sender, RoutedEventArgs e) => ZoomSlider.Value = Math.Max(25, ZoomSlider.Value - 10);
    private void ZoomFit_Click(object sender, RoutedEventArgs e) { ZoomSlider.Value = 75; CanvasScroll.ScrollToHome(); }
    private void CanvasScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) { ZoomSlider.Value = Math.Clamp(ZoomSlider.Value + (e.Delta > 0 ? 10 : -10), 25, 200); e.Handled = true; }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            switch (e.Key)
            {
                case Key.C: CopySelected(); e.Handled = true; break;
                case Key.X: CopySelected(); DeleteSelected(); e.Handled = true; break;
                case Key.V: PasteSelected(); e.Handled = true; break;
                case Key.D: Duplicate_Click(this, new RoutedEventArgs()); e.Handled = true; break;
                case Key.Z: Undo_Click(this, new RoutedEventArgs()); e.Handled = true; break;
                case Key.S: SaveProject_Click(this, new RoutedEventArgs()); e.Handled = true; break;
                case Key.O: OpenProject_Click(this, new RoutedEventArgs()); e.Handled = true; break;
            }
        }
        else if (e.Key == Key.Delete) { DeleteSelected(); e.Handled = true; }
        else if (e.Key == Key.Escape) { SelectElement(null); SelectTool.IsChecked = true; }
    }

    private void Window_DragOver(object sender, DragEventArgs e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
    private void Window_Drop(object sender, DragEventArgs e) => HandleFileDrop(e, new Point(80, 80));
    private void Canvas_Drop(object sender, DragEventArgs e) => HandleFileDrop(e, e.GetPosition(DrawingCanvas));
    private void HandleFileDrop(DragEventArgs e, Point point)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        e.Handled = true;
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            var offset = 0;
            foreach (var file in files) if (File.Exists(file) && IsImage(file)) { try { AddImage(file, point.X + offset, point.Y + offset); offset += 25; } catch { } }
            StatusLabel.Text = "Dropped files added to the workspace";
        }
    }
    private static bool IsImage(string path) => new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff" }.Contains(System.IO.Path.GetExtension(path).ToLowerInvariant());

    private void SaveProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Title = "Save editable anno+ project", Filter = "anno+ project|*.annoplus", FileName = "study-workspace.annoplus" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var objects = DrawingCanvas.Children.Cast<UIElement>().Select(el => new ProjectObject { Xaml = XamlWriter.Save(el), Left = double.IsFinite(Canvas.GetLeft(el)) ? Canvas.GetLeft(el) : 0, Top = double.IsFinite(Canvas.GetTop(el)) ? Canvas.GetTop(el) : 0 }).ToList();
            File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(new ProjectData { Width = DrawingCanvas.Width, Height = DrawingCanvas.Height, Objects = objects }, new JsonSerializerOptions { WriteIndented = true }));
            _projectPath = dialog.FileName; DocumentLabel.Text = System.IO.Path.GetFileName(dialog.FileName); StatusLabel.Text = "Editable project saved";
        }
        catch (Exception ex) { MessageBox.Show($"Could not save project.\n{ex.Message}", "anno+", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Open anno+ project", Filter = "anno+ project|*.annoplus|JSON files|*.json" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var data = JsonSerializer.Deserialize<ProjectData>(File.ReadAllText(dialog.FileName)) ?? throw new InvalidDataException("The project file is empty.");
            DrawingCanvas.Children.Clear(); _undo.Clear(); SelectElement(null); DrawingCanvas.Width = data.Width; DrawingCanvas.Height = data.Height;
            foreach (var item in data.Objects)
            {
                if (XamlReader.Parse(item.Xaml) is UIElement element) { Canvas.SetLeft(element, item.Left); Canvas.SetTop(element, item.Top); DrawingCanvas.Children.Add(element); }
            }
            _projectPath = dialog.FileName; DocumentLabel.Text = System.IO.Path.GetFileName(dialog.FileName); StatusLabel.Text = $"Opened project with {DrawingCanvas.Children.Count} objects";
        }
        catch (Exception ex) { MessageBox.Show($"Could not open project.\n{ex.Message}", "anno+", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private sealed class ProjectData { public double Width { get; set; } = 1600; public double Height { get; set; } = 1100; public List<ProjectObject> Objects { get; set; } = new(); }
    private sealed class ProjectObject { public string Xaml { get; set; } = ""; public double Left { get; set; } public double Top { get; set; } }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas.Children.Count == 0) { MessageBox.Show("Add images, text or annotations before exporting.", "anno+"); return; }
        var dialog = new SaveFileDialog { Title = "Export workspace as PNG", Filter = "PNG image|*.png", FileName = "anno-plus-workspace.png" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var width = (int)DrawingCanvas.Width; var height = (int)DrawingCanvas.Height;
            DrawingCanvas.Measure(new Size(width, height)); DrawingCanvas.Arrange(new Rect(0, 0, width, height));
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(DrawingCanvas);
            using var stream = File.Create(dialog.FileName); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); encoder.Save(stream);
            StatusLabel.Text = "PNG exported successfully";
        }
        catch (Exception ex) { MessageBox.Show($"Export failed.\n{ex.Message}", "anno+", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
}