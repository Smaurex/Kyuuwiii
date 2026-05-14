using Kyuuwiii.Services;

namespace Kyuuwiii;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Initialize DB synchronously before any page loads
        Task.Run(async () =>
            await DatabaseService.Instance.InitAsync()
        ).Wait();

        MainPage = new AppShell();
    }
}
