using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LabWorksApp.Views.Components;

public partial class FunctionPlotCanvas : UserControl
{
    private static readonly SolidColorBrush AxisBrush = new(Color.FromRgb(148, 163, 184));      // #94A3B8
    private static readonly SolidColorBrush GridBrush = new(Color.FromRgb(30, 41, 59));        // #1E293B
    private static readonly SolidColorBrush CurveBrush = new(Color.FromRgb(56, 189, 248));     // #38BDF8
    private static readonly SolidColorBrush IntervalBrush = new(Color.FromArgb(50, 30, 58, 138)); // #1E3A8A
    private static readonly SolidColorBrush MinMarkerBrush = new(Color.FromRgb(16, 185, 129)); // #10B981
    private static readonly SolidColorBrush RootMarkerBrush = new(Color.FromRgb(245, 158, 11));// #F59E0B
    private static readonly SolidColorBrush LabelTextBrush = new(Color.FromRgb(203, 213, 225));// #CBD5E1

    private Func<double, double>? _function;
    private double _viewA = -5.0;
    private double _viewB = 5.0;
    private double _minX = -5.0;
    private double _maxX = 5.0;
    private double _minY = -5.0;
    private double _maxY = 5.0;

    private double? _pointMinX;
    private double? _pointMinY;
    private double? _pointRootX;
    private double? _pointRootY;

    public FunctionPlotCanvas()
    {
        InitializeComponent();
    }

    public void Plot(Func<double, double> func, double a, double b,
                     double? minX = null, double? minY = null,
                     double? rootX = null, double? rootY = null)
    {
        _function = func;
        _viewA = Math.Min(a, b);
        _viewB = Math.Max(a, b);
        _pointMinX = minX;
        _pointMinY = minY;
        _pointRootX = rootX;
        _pointRootY = rootY;

        AutoFitView();
        Redraw();
    }

    public void Clear()
    {
        _function = null;
        _pointMinX = null;
        _pointMinY = null;
        _pointRootX = null;
        _pointRootY = null;
        PlotCanvas.Children.Clear();
    }

    private void AutoFitView()
    {
        if (_function == null) return;

        double span = Math.Abs(_viewB - _viewA);
        if (span < 1e-6) span = 1.0;

        // Расширяем видимую область вокруг [a, b] на 30% с каждой стороны
        _minX = _viewA - span * 0.3;
        _maxX = _viewB + span * 0.3;

        // Вычисляем разброс Y по точкам
        int samples = 100;
        double step = (_maxX - _minX) / samples;
        double yMin = double.MaxValue;
        double yMax = double.MinValue;

        for (int i = 0; i <= samples; i++)
        {
            double x = _minX + i * step;
            try
            {
                double y = _function(x);
                if (!double.IsNaN(y) && !double.IsInfinity(y))
                {
                    if (y < yMin) yMin = y;
                    if (y > yMax) yMax = y;
                }
            }
            catch
            {
                // Игнорируем особые точки
            }
        }

        if (_pointMinY.HasValue && !double.IsNaN(_pointMinY.Value))
        {
            yMin = Math.Min(yMin, _pointMinY.Value);
            yMax = Math.Max(yMax, _pointMinY.Value);
        }

        if (yMin >= yMax || double.IsInfinity(yMin) || double.IsInfinity(yMax))
        {
            yMin = -5.0;
            yMax = 5.0;
        }

        double ySpan = yMax - yMin;
        if (ySpan < 1e-4) ySpan = 2.0;

        _minY = yMin - ySpan * 0.2;
        _maxY = yMax + ySpan * 0.2;
    }

    private void Redraw()
    {
        PlotCanvas.Children.Clear();

        double w = PlotCanvas.ActualWidth;
        double h = PlotCanvas.ActualHeight;
        if (w <= 10 || h <= 10) return;

        // 1. Отрисовка координатной сетки и осей
        DrawGridAndAxes(w, h);

        // 2. Отрисовка затененного интервала [a, b]
        DrawInterval(w, h);

        // 3. Отрисовка графика функции f(x)
        if (_function != null)
        {
            DrawCurve(w, h);
        }

        // 4. Отрисовка маркера локального минимума
        if (_pointMinX.HasValue && _pointMinY.HasValue)
        {
            DrawMarker(_pointMinX.Value, _pointMinY.Value, w, h, MinMarkerBrush, $"Min ({_pointMinX.Value:F3}; {_pointMinY.Value:F3})");
        }

        // 5. Отрисовка маркера корня
        if (_pointRootX.HasValue && _pointRootY.HasValue)
        {
            DrawMarker(_pointRootX.Value, _pointRootY.Value, w, h, RootMarkerBrush, $"Корень x={_pointRootX.Value:F3}");
        }
    }

    private void DrawGridAndAxes(double w, double h)
    {
        // Вычисляем шаг сетки
        double xSpan = _maxX - _minX;
        double ySpan = _maxY - _minY;
        if (xSpan <= 0 || ySpan <= 0) return;

        double xStep = CalculateNiceStep(xSpan / 8.0);
        double yStep = CalculateNiceStep(ySpan / 8.0);

        // Вертикальные линии сетки
        double firstX = Math.Ceiling(_minX / xStep) * xStep;
        for (double x = firstX; x <= _maxX; x += xStep)
        {
            double sx = ToScreenX(x, w);
            var line = new Line
            {
                X1 = sx, Y1 = 0,
                X2 = sx, Y2 = h,
                Stroke = GridBrush,
                StrokeThickness = 1
            };
            PlotCanvas.Children.Add(line);

            // Числовая метка
            var lbl = new TextBlock
            {
                Text = x.ToString("G4"),
                FontSize = 9,
                Foreground = LabelTextBrush,
                Opacity = 0.7
            };
            Canvas.SetLeft(lbl, sx + 2);
            Canvas.SetTop(lbl, h - 14);
            PlotCanvas.Children.Add(lbl);
        }

        // Горизонтальные линии сетки
        double firstY = Math.Ceiling(_minY / yStep) * yStep;
        for (double y = firstY; y <= _maxY; y += yStep)
        {
            double sy = ToScreenY(y, h);
            var line = new Line
            {
                X1 = 0, Y1 = sy,
                X2 = w, Y2 = sy,
                Stroke = GridBrush,
                StrokeThickness = 1
            };
            PlotCanvas.Children.Add(line);

            // Числовая метка
            var lbl = new TextBlock
            {
                Text = y.ToString("G4"),
                FontSize = 9,
                Foreground = LabelTextBrush,
                Opacity = 0.7
            };
            Canvas.SetLeft(lbl, 4);
            Canvas.SetTop(lbl, sy - 12);
            PlotCanvas.Children.Add(lbl);
        }

        // Ось Y (x = 0)
        if (_minX <= 0 && _maxX >= 0)
        {
            double sx0 = ToScreenX(0, w);
            var yAxis = new Line
            {
                X1 = sx0, Y1 = 0,
                X2 = sx0, Y2 = h,
                Stroke = AxisBrush,
                StrokeThickness = 1.5
            };
            PlotCanvas.Children.Add(yAxis);
        }

        // Ось X (y = 0)
        if (_minY <= 0 && _maxY >= 0)
        {
            double sy0 = ToScreenY(0, h);
            var xAxis = new Line
            {
                X1 = 0, Y1 = sy0,
                X2 = w, Y2 = sy0,
                Stroke = AxisBrush,
                StrokeThickness = 1.5
            };
            PlotCanvas.Children.Add(xAxis);
        }
    }

    private void DrawInterval(double w, double h)
    {
        double sx1 = Math.Clamp(ToScreenX(_viewA, w), 0, w);
        double sx2 = Math.Clamp(ToScreenX(_viewB, w), 0, w);

        double left = Math.Min(sx1, sx2);
        double width = Math.Abs(sx2 - sx1);

        if (width > 0)
        {
            var rect = new Rectangle
            {
                Width = width,
                Height = h,
                Fill = IntervalBrush
            };
            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, 0);
            PlotCanvas.Children.Add(rect);

            // Вертикальные границы a и b
            var borderA = new Line { X1 = sx1, Y1 = 0, X2 = sx1, Y2 = h, Stroke = CurveBrush, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 4, 2 } };
            var borderB = new Line { X1 = sx2, Y1 = 0, X2 = sx2, Y2 = h, Stroke = CurveBrush, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 4, 2 } };
            PlotCanvas.Children.Add(borderA);
            PlotCanvas.Children.Add(borderB);
        }
    }

    private void DrawCurve(double w, double h)
    {
        if (_function == null) return;

        var pathFigure = new PathFigure();
        var pathGeometry = new PathGeometry();
        bool isFirst = true;

        // Дискретизация кривой: 1 точка на каждые 2 пикселя экрана
        int pixelSteps = (int)Math.Max(100, w / 2);
        double dx = (_maxX - _minX) / pixelSteps;

        for (int i = 0; i <= pixelSteps; i++)
        {
            double x = _minX + i * dx;
            try
            {
                double y = _function(x);
                if (double.IsNaN(y) || double.IsInfinity(y))
                {
                    isFirst = true;
                    continue;
                }

                double sx = ToScreenX(x, w);
                double sy = ToScreenY(y, h);

                // Ограничиваем сильные выбросы за пределы холста
                sy = Math.Clamp(sy, -h * 2, h * 3);

                if (isFirst)
                {
                    pathFigure.StartPoint = new Point(sx, sy);
                    isFirst = false;
                }
                else
                {
                    pathFigure.Segments.Add(new LineSegment(new Point(sx, sy), true));
                }
            }
            catch
            {
                isFirst = true;
            }
        }

        pathGeometry.Figures.Add(pathFigure);
        var path = new Path
        {
            Stroke = CurveBrush,
            StrokeThickness = 2.2,
            Data = pathGeometry
        };
        PlotCanvas.Children.Add(path);
    }

    private void DrawMarker(double mathX, double mathY, double w, double h, Brush brush, string label)
    {
        double sx = ToScreenX(mathX, w);
        double sy = ToScreenY(mathY, h);

        if (sx < -20 || sx > w + 20 || sy < -20 || sy > h + 20) return;

        double radius = 5.0;
        var circle = new Ellipse
        {
            Width = radius * 2,
            Height = radius * 2,
            Fill = brush,
            Stroke = Brushes.White,
            StrokeThickness = 1.5
        };
        Canvas.SetLeft(circle, sx - radius);
        Canvas.SetTop(circle, sy - radius);
        PlotCanvas.Children.Add(circle);

        var textBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(220, 15, 23, 42)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(4, 2, 4, 2),
            BorderBrush = brush,
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = label,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            }
        };

        Canvas.SetLeft(textBorder, Math.Clamp(sx + 8, 4, w - 140));
        Canvas.SetTop(textBorder, Math.Clamp(sy - 22, 4, h - 26));
        PlotCanvas.Children.Add(textBorder);
    }

    private double ToScreenX(double mathX, double screenWidth) =>
        (mathX - _minX) / (_maxX - _minX) * screenWidth;

    private double ToScreenY(double mathY, double screenHeight) =>
        screenHeight - (mathY - _minY) / (_maxY - _minY) * screenHeight;

    private double ToMathX(double screenX, double screenWidth) =>
        _minX + screenX / screenWidth * (_maxX - _minX);

    private double ToMathY(double screenY, double screenHeight) =>
        _minY + (screenHeight - screenY) / screenHeight * (_maxY - _minY);

    private static double CalculateNiceStep(double roughStep)
    {
        double power = Math.Pow(10, Math.Floor(Math.Log10(roughStep)));
        double fraction = roughStep / power;

        double niceFraction = fraction switch
        {
            < 1.5 => 1.0,
            < 3.0 => 2.0,
            < 7.0 => 5.0,
            _ => 10.0
        };

        return niceFraction * power;
    }

    private void PlotCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(PlotCanvas);
        double mathX = ToMathX(pos.X, PlotCanvas.ActualWidth);
        double mathY = ToMathY(pos.Y, PlotCanvas.ActualHeight);

        TxtCursorCoord.Text = $"x: {mathX:F3}, y: {mathY:F3}";
        BorderCursorCoord.Visibility = Visibility.Visible;
    }

    private void PlotCanvas_MouseLeave(object sender, MouseEventArgs e)
    {
        BorderCursorCoord.Visibility = Visibility.Collapsed;
    }

    private void PlotCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Redraw();
    }

    private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
    {
        Zoom(0.7);
    }

    private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
    {
        Zoom(1.4);
    }

    private void BtnFitView_Click(object sender, RoutedEventArgs e)
    {
        AutoFitView();
        Redraw();
    }

    private void Zoom(double factor)
    {
        double cx = (_minX + _maxX) / 2.0;
        double cy = (_minY + _maxY) / 2.0;
        double halfW = (_maxX - _minX) * factor / 2.0;
        double halfH = (_maxY - _minY) * factor / 2.0;

        _minX = cx - halfW;
        _maxX = cx + halfW;
        _minY = cy - halfH;
        _maxY = cy + halfH;

        Redraw();
    }
}
