using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AnnoPlus;

public partial class MainWindow : Window
{
    private string _tool = "Pen";
    private Brush _ink = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4778E8"));
    private bool _drawing;
    private Point _start;
    private UIElement? _preview;
    private readonly Stack<UIElement> _undo = new();
    private BitmapSource? _sourceImage;

    public MainWindow() => InitializeComponent();

    private void OpenImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open an image to annotate",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|All files|*.*"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(dialog.FileName);
            image.EndInit();
            _sourceImage = image;
            DrawingCanvas.Children.Clear();
            _undo.Clear();
            var display = new Image { Source = image, Width = image.PixelWidth, Height = image.PixelHeight, Stretch = Stretch.None, IsHitTestVisible = false };
            DrawingCanvas.Children.Add(display);
            _undo.Clear();
            DrawingCanvas.Width = Math.Max(900, image.PixelWidth);
            DrawingCanvas.Height = Math.Max(620, image.PixelHeight);
            DocumentLabel.Text = System.IO.Path.GetFileName(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open image.\n{ex.Message}", "anno+", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Tool_Checked(object sender, RoutedEventArgs e)
    {
        if (PenTool?.IsChecked == true) _tool = "Pen";
        else if (HighlightTool?.IsChecked == true) _tool = "Highlight";
        else if (ArrowTool?.IsChecked == true) _tool = "Arrow";
        else if (RectTool?.IsChecked == true) _tool = "Rectangle";
        else if (EllipseTool?.IsChecked == true) _tool = "Ellipse";
    }

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string color)
            _ink = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_sourceImage is null)
        {
            DocumentLabel.Text = "Open an image to start";
            return;
        }
        _drawing = true;
        _start = e.GetPosition(DrawingCanvas);
        DrawingCanvas.CaptureMouse();
        if (_tool == "Pen" || _tool == "Highlight")
        {
            var stroke = new Polyline
            {
                Stroke = _tool == "Highlight" ? new SolidColorBrush(Color.FromArgb(100, ((SolidColorBrush)_ink).Color.R, ((SolidColorBrush)_ink).Color.G, ((SolidColorBrush)_ink).Color.B)) : _ink,
                StrokeThickness = _tool == "Highlight" ? Math.Max(14, WidthSlider.Value * 4) : WidthSlider.Value,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Points = new PointCollection { _start }
            };
            _preview = stroke;
            DrawingCanvas.Children.Add(stroke);
        }
        else
        {
            Shape shape = _tool == "Ellipse" ? new Ellipse() : _tool == "Rectangle" ? new Rectangle() : new Line();
            shape.Stroke = _ink;
            shape.StrokeThickness = WidthSlider.Value;
            if (shape is Line line) { line.X1 = _start.X; line.Y1 = _start.Y; line.X2 = _start.X; line.Y2 = _start.Y; line.StrokeEndLineCap = PenLineCap.Triangle; }
            else shape.Fill = Brushes.Transparent;
            _preview = shape;
            DrawingCanvas.Children.Add(shape);
        }
        e.Handled = true;
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_drawing || _preview is null) return;
        var p = e.GetPosition(DrawingCanvas);
        if (_preview is Polyline polyline) polyline.Points.Add(p);
        else if (_preview is Line line) { line.X2 = p.X; line.Y2 = p.Y; }
        else if (_preview is Shape shape)
        {
            Canvas.SetLeft(shape, Math.Min(_start.X, p.X));
            Canvas.SetTop(shape, Math.Min(_start.Y, p.Y));
            shape.Width = Math.Abs(p.X - _start.X);
            shape.Height = Math.Abs(p.Y - _start.Y);
        }
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_drawing) return;
        _drawing = false;
        DrawingCanvas.ReleaseMouseCapture();
        if (_preview is not null) _undo.Push(_preview);
        _preview = null;
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (_undo.Count == 0) return;
        var element = _undo.Pop();
        if (DrawingCanvas.Children.Contains(element)) DrawingCanvas.Children.Remove(element);
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (_sourceImage is null) { DrawingCanvas.Children.Clear(); return; }
        DrawingCanvas.Children.Clear();
        DrawingCanvas.Children.Add(new Image { Source = _sourceImage, Width = _sourceImage.PixelWidth, Height = _sourceImage.PixelHeight, Stretch = Stretch.None, IsHitTestVisible = false });
        _undo.Clear();
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas.Children.Count == 0)
        {
            MessageBox.Show("Open an image or add annotations first.", "anno+");
            return;
        }
        var dialog = new SaveFileDialog { Title = "Export annotated image", Filter = "PNG image|*.png", FileName = "anno-plus-annotation.png" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            DrawingCanvas.Measure(new Size(DrawingCanvas.Width, DrawingCanvas.Height));
            DrawingCanvas.Arrange(new Rect(0, 0, DrawingCanvas.Width, DrawingCanvas.Height));
            var bitmap = new RenderTargetBitmap((int)DrawingCanvas.Width, (int)DrawingCanvas.Height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(DrawingCanvas);
            using var stream = File.Create(dialog.FileName);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream);
            MessageBox.Show("Your annotated image has been exported.", "anno+", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed.\n{ex.Message}", "anno+", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
