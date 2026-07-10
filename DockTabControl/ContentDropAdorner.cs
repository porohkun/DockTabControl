namespace DockTabControl;

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

/// <summary>
/// Полупрозрачная заливка с рамкой поверх зоны контента —
/// показывает, что дроп поместит вкладку в конец списка.
/// </summary>
public sealed class ContentDropAdorner : Adorner
{
    private readonly Brush _fill;
    private readonly Pen _pen;

    public ContentDropAdorner(UIElement adornedElement)
        : base(adornedElement)
    {
        IsHitTestVisible = false;
        _fill = new SolidColorBrush(Color.FromArgb(0x22, 0x2C, 0x7B, 0xE0));
        _fill.Freeze();
        _pen = new(new SolidColorBrush(Color.FromRgb(0x2C, 0x7B, 0xE0)), 1.5);
        _pen.Freeze();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var element = (FrameworkElement)AdornedElement;
        var rect = new Rect(0, 0, element.ActualWidth, element.ActualHeight);
        rect.Inflate(-1, -1); // чтобы рамка не срезалась по краю слоя
        drawingContext.DrawRectangle(_fill, _pen, rect);
    }
}