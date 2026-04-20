using FinanceApp.Data;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FinanceApp.Views;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

namespace FinanceApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.AddSingleton<IDatabaseContext, DatabaseContext>();
        builder.Services.AddSingleton<IFinanceService, FinanceService>();

        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<AddEntryViewModel>();

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<AddEntryPopup>();

        return builder.Build();
    }
}