namespace DockTabControl;

using System.Windows.Controls;

/// <summary>
/// Данные, которые едут в DataObject во время перетаскивания вкладки.
/// </summary>
public sealed class TabDragData
{
    public TabDragData(DockTabControl sourceControl, TabItem tab, object? item)
    {
        SourceControl = sourceControl;
        Tab = tab;
        Item = item;
    }

    /// <summary>Контрол, из которого потянули вкладку.</summary>
    public DockTabControl SourceControl { get; }

    /// <summary>Сам контейнер вкладки.</summary>
    public TabItem Tab { get; }

    /// <summary>
    /// Модель (ItemsSource), если контрол работает через привязку.
    /// Null, если вкладки заданы напрямую как TabItem'ы.
    /// </summary>
    public object? Item { get; }
}