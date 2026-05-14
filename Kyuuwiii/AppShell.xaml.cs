using Kyuuwiii.Pages;

namespace Kyuuwiii;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register modal/push routes
        Routing.RegisterRoute("RegisterPage", typeof(RegisterPage));
    }
}
