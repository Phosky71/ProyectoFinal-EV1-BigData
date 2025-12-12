using Microsoft.UI.Xaml;

namespace MauiApp.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override Microsoft.Maui.Hosting.MauiApp CreateMauiApp()
        => MauiApp.UI.MauiProgram.CreateMauiApp();
}
