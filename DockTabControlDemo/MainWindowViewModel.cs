namespace DockTabControlDemo;

using System.Collections.ObjectModel;

internal class MainWindowViewModel : BindableBase
{
    public ObservableCollection<TabViewModel> LeftTabs { get; } =
    [
        new() { Header = "Tab 1", Body = "Tab 1 body" },
        new() { Header = "Tab 2", Body = "Tab 2 body" },
        new() { Header = "Tab 3", Body = "Tab 3 body" }
    ];

    public ObservableCollection<TabViewModel> RightTabs { get; } =
    [
        new() { Header = "Tab 4", Body = "Tab 4 body" },
        new() { Header = "Tab 5", Body = "Tab 5 body" },
        new() { Header = "Tab 6", Body = "Tab 6 body" }
    ];
}