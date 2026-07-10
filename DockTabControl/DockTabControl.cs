namespace DockTabControl;

using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ModPlus.GongSolutions.Wpf.DragDrop;
using GongDragDrop = ModPlus.GongSolutions.Wpf.DragDrop.DragDrop;

/// <summary>
/// TabControl с поддержкой перетаскивания вкладок: реордер внутри себя
/// и перенос вкладок между разными DockTabControl.
/// </summary>
public class DockTabControl : TabControl, IDragSource, IDropTarget
{
    private InsertionAdorner? _insertionAdorner;
    private ContentDropAdorner? _contentAdorner;
    private AdornerLayer? _adornerLayer;
    private Border? _contentHost;

    static DockTabControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(DockTabControl),
            new FrameworkPropertyMetadata(typeof(DockTabControl)));
    }

    public DockTabControl()
    {
        GongDragDrop.SetIsDragSource(this, true);
        GongDragDrop.SetIsDropTarget(this, true);
        GongDragDrop.SetDragHandler(this, this);
        GongDragDrop.SetDropHandler(this, this);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _contentHost = GetTemplateChild("PART_ContentHost") as Border;
    }

    private static bool IsAfterMidpoint(IDropInfo dropInfo, TabItem targetTab)
    {
        Point pos = dropInfo.DropPosition; // в координатах VisualTarget
        // Переводим в координаты вкладки.
        Point local = ((UIElement)dropInfo.VisualTarget)
            .TransformToVisual(targetTab)
            .Transform(pos);
        return local.X > targetTab.ActualWidth / 2.0;
    }

    // ------------------------------------------------------------------
    // Хелперы поиска в визуальном дереве
    // ------------------------------------------------------------------

    private static TabDragData? GetPayload(IDropInfo dropInfo)
        => dropInfo.Data as TabDragData
           ?? dropInfo.DragInfo?.Data as TabDragData;

    private static TabItem? FindTabItem(DependencyObject? source)
    {
        while (source is not null and not TabItem)
        {
            source = source is Visual or Visual3D
                ? VisualTreeHelper.GetParent(source)
                : LogicalTreeHelper.GetParent(source);
        }

        return source as TabItem;
    }

    /// <summary>
    /// true, если элемент лежит в заголовочной части вкладки, а не в её контенте.
    /// Опираемся на то, что заголовок находится внутри TabItem, но вне
    /// ContentPresenter с содержимым (который в шаблоне TabControl лежит отдельно).
    /// </summary>
    private static bool IsWithinHeader(DependencyObject? source)
    {
        var node = source;
        bool sawTabItem = false;
        while (node is not null)
        {
            if (node is ContentPresenter cp && cp.Name == "PART_SelectedContentHost")
                return false; // это тело выбранной вкладки

            if (node is TabItem)
            {
                sawTabItem = true;
                break;
            }

            node = node is Visual or Visual3D
                ? VisualTreeHelper.GetParent(node)
                : LogicalTreeHelper.GetParent(node);
        }

        return sawTabItem;
    }

    private static object GetItemForContainer(TabItem container, DockTabControl owner)
    {
        object item = owner.ItemContainerGenerator.ItemFromContainer(container);
        return item == DependencyProperty.UnsetValue ? container : item;
    }

    private void MoveTab(TabDragData payload, int targetIndex)
    {
        bool sameControl = ReferenceEquals(payload.SourceControl, this);

        if (payload.Item is not null)
            MoveBoundItem(payload, targetIndex, sameControl);
        else
            MoveDirectTab(payload, targetIndex, sameControl);
    }

    /// <summary>Режим ItemsSource: двигаем модель в коллекции.</summary>
    private void MoveBoundItem(TabDragData payload, int targetIndex, bool sameControl)
    {
        var srcList = payload.SourceControl.ItemsSource as IList;
        var dstList = ItemsSource as IList;
        if (srcList is null || dstList is null)
            return;

        object item = payload.Item!;

        if (sameControl)
        {
            int oldIndex = dstList.IndexOf(item);
            if (oldIndex < 0) return;

            // Индекс сдвигается, если удаляем элемент левее точки вставки.
            if (oldIndex < targetIndex)
                targetIndex--;

            if (oldIndex == targetIndex)
                return;

            dstList.RemoveAt(oldIndex);
            targetIndex = Math.Clamp(targetIndex, 0, dstList.Count);
            dstList.Insert(targetIndex, item);
        }
        else
        {
            srcList.Remove(item);
            targetIndex = Math.Clamp(targetIndex, 0, dstList.Count);
            dstList.Insert(targetIndex, item);
        }

        SelectedItem = item;
    }

    /// <summary>Режим прямых TabItem: перевешиваем контейнер.</summary>
    private void MoveDirectTab(TabDragData payload, int targetIndex, bool sameControl)
    {
        var tab = payload.Tab;

        if (sameControl)
        {
            int oldIndex = Items.IndexOf(tab);
            if (oldIndex < 0) return;

            if (oldIndex < targetIndex)
                targetIndex--;

            if (oldIndex == targetIndex)
                return;

            Items.Remove(tab);
            targetIndex = Math.Clamp(targetIndex, 0, Items.Count);
            Items.Insert(targetIndex, tab);
        }
        else
        {
            payload.SourceControl.Items.Remove(tab);
            targetIndex = Math.Clamp(targetIndex, 0, Items.Count);
            Items.Insert(targetIndex, tab);
        }

        SelectedItem = tab;
    }

    // ------------------------------------------------------------------
    // Адорнер вставки
    // ------------------------------------------------------------------

    private void ShowInsertionAdorner(IDropInfo dropInfo, TabItem targetTab)
    {
        _adornerLayer ??= AdornerLayer.GetAdornerLayer(this);
        if (_adornerLayer is null)
            return;

        RemoveContentAdorner();

        if (_insertionAdorner is null
            || !ReferenceEquals(_insertionAdorner.AdornedElement, targetTab))
        {
            RemoveInsertionAdorner();
            _insertionAdorner = new(targetTab);
            _adornerLayer.Add(_insertionAdorner);
        }

        _insertionAdorner.IsLeftSide = !IsAfterMidpoint(dropInfo, targetTab);
    }

    private void ShowContentAdorner()
    {
        _adornerLayer ??= AdornerLayer.GetAdornerLayer(this);
        if (_adornerLayer is null)
            return;

        RemoveInsertionAdorner();

        UIElement host = _contentHost as UIElement ?? this;
        if (_contentAdorner is null
            || !ReferenceEquals(_contentAdorner.AdornedElement, host))
        {
            RemoveContentAdorner();
            _contentAdorner = new(host);
            _adornerLayer.Add(_contentAdorner);
        }
    }

    private void RemoveInsertionAdorner()
    {
        if (_insertionAdorner is not null && _adornerLayer is not null)
            _adornerLayer.Remove(_insertionAdorner);
        _insertionAdorner = null;
    }

    private void RemoveContentAdorner()
    {
        if (_contentAdorner is not null && _adornerLayer is not null)
            _adornerLayer.Remove(_contentAdorner);
        _contentAdorner = null;
    }

    private void RemoveAdorners()
    {
        RemoveInsertionAdorner();
        RemoveContentAdorner();
    }

    #region IDragSource

    // ------------------------------------------------------------------
    // IDragSource — что и когда можно тянуть (явная реализация)
    // ------------------------------------------------------------------

    void IDragSource.StartDrag(IDragInfo dragInfo)
    {
        var tab = FindTabItem(dragInfo.VisualSourceItem);
        if (tab is null)
            return;

        if (!IsWithinHeader(dragInfo.VisualSourceItem))
            return;

        object? item = ItemContainerGenerator.ItemFromContainer(tab);
        if (item == tab)
            item = null;

        var payload = new TabDragData(this, tab, item);
        dragInfo.Data = payload;
        dragInfo.DataObject = new DataObject(typeof(TabDragData), payload);
        dragInfo.Effects = DragDropEffects.Move;
    }

    bool IDragSource.CanStartDrag(IDragInfo dragInfo)
        => IsWithinHeader(dragInfo.VisualSourceItem)
           && FindTabItem(dragInfo.VisualSourceItem) is not null;

    void IDragSource.Dropped(IDropInfo dropInfo)
    {
    }

    void IDragSource.DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo)
    {
    }

    void IDragSource.DragCancelled() => RemoveAdorners();

    bool IDragSource.TryCatchOccurredException(Exception exception) => false;

    #endregion //IDragSource

    #region IDropTarget

    // ------------------------------------------------------------------
    // IDropTarget — куда можно бросить (явная реализация)
    // ------------------------------------------------------------------

    void IDropTarget.DragEnter(IDropInfo dropInfo)
    {
    }

    void IDropTarget.DragOver(IDropInfo dropInfo)
    {
        if (GetPayload(dropInfo) is null)
            return;

        dropInfo.Effects = DragDropEffects.Move;
        dropInfo.DropTargetHintAdorner = null; // глушим стандартный хинт Gong в обоих случаях

        var targetTab = FindTabItem(dropInfo.VisualTargetItem);
        bool overHeader = targetTab is not null
                          || IsWithinHeader(dropInfo.VisualTargetItem);

        if (overHeader && targetTab is not null)
            ShowInsertionAdorner(dropInfo, targetTab);
        else
            ShowContentAdorner();

        dropInfo.NotHandled = false;
    }

    void IDropTarget.DragLeave(IDropInfo dropInfo) => RemoveAdorners();

    void IDropTarget.DropHint(IDropHintInfo dropHintInfo)
    {
    }

    void IDropTarget.Drop(IDropInfo dropInfo)
    {
        RemoveAdorners();

        var payload = GetPayload(dropInfo);
        if (payload is null)
            return;

        var targetTab = FindTabItem(dropInfo.VisualTargetItem);
        bool overHeader = targetTab is not null
                          || IsWithinHeader(dropInfo.VisualTargetItem);

        int insertIndex;
        if (overHeader && targetTab is not null)
        {
            insertIndex = Items.IndexOf(GetItemForContainer(targetTab, this));
            if (IsAfterMidpoint(dropInfo, targetTab))
                insertIndex++;
        }
        else
        {
            insertIndex = Items.Count;
        }

        MoveTab(payload, insertIndex);
    }

    #endregion //IDropTarget
}