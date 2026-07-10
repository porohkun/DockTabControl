namespace DockTabControl;

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

/// <summary>
/// Вертикальная линия, показывающая, куда встанет вкладка при дропе в полосу заголовков.
/// </summary>
public sealed class InsertionAdorner : Adorner
{
    private readonly Pen _pen;
    private bool _isLeftSide;

    public InsertionAdorner(UIElement adornedElement)
        : base(adornedElement)
    {
        IsHitTestVisible = false;
        _pen = new(new SolidColorBrush(Color.FromRgb(0x2C, 0x7B, 0xE0)), 2.0);
        _pen.Freeze();
    }

    /// <summary>true — линия слева от элемента, false — справа.</summary>
    public bool IsLeftSide
    {
        get => _isLeftSide;
        set
        {
            if (_isLeftSide == value) return;
            _isLeftSide = value;
            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var element = (FrameworkElement)AdornedElement;
        double x = _isLeftSide ? 0 : element.ActualWidth;
        var start = new Point(x, 0);
        var end = new Point(x, element.ActualHeight);
        drawingContext.DrawLine(_pen, start, end);
    }
}