using Microsoft.Extensions.Logging;

namespace Kyuuwiii;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddTransient<Pages.LoginPage>();
        builder.Services.AddTransient<Pages.RegisterPage>();
        builder.Services.AddTransient<Pages.StudentDashboard>();
        builder.Services.AddTransient<Pages.QueueStatusPage>();
        builder.Services.AddTransient<Pages.ManagerDashboard>();
        builder.Services.AddTransient<Pages.AdminDashboard>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
