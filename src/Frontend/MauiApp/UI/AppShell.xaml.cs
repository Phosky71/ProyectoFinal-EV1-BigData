namespace MauiApp.UI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("//LoginPage", typeof(LoginPage));
        Routing.RegisterRoute("//MasterPage", typeof(MasterPage));
        Routing.RegisterRoute("DetailPage", typeof(DetailPage));
        Routing.RegisterRoute("MCPPage", typeof(MCPPage));
    }
}
