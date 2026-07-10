namespace DockTabControlDemo;

internal class TabViewModel : BindableBase
{
    public string Header
    {
        get;
        set => Set(ref field, value);
    } = "";

    public string Body
    {
        get;
        set => Set(ref field, value);
    } = "";
}