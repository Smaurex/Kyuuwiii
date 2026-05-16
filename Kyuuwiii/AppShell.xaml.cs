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

    /// <summary>
    /// Hard-resets the app back to the login screen by replacing MainPage.
    /// Use this instead of GoToAsync("//LoginPage") to guarantee a clean stack.
    /// </summary>
    public static void NavigateToLogin()
    {
        Application.Current!.MainPage = new AppShell();
    }
}
