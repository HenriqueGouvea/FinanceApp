using FinanceApp.Data;
using FinanceApp.Controls;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FinanceApp.Views;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.Messaging;

namespace FinanceApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        AppCulture.Apply();

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler<SwipePager, SwipePagerHandler>();
#endif
            });
#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.AddSingleton<IDatabaseContext, DatabaseContext>();
        builder.Services.AddSingleton<IFinanceService, FinanceService>();
        builder.Services.AddSingleton<IPreferencesService, PreferencesService>();
        builder.Services.AddSingleton<ICategoryService, CategoryService>();
        builder.Services.AddSingleton<IPaymentSourceService, PaymentSourceService>();
        builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<AddEntryViewModel>();
        builder.Services.AddTransient<CategoriesViewModel>();
        builder.Services.AddTransient<CategoryEditViewModel>();
        builder.Services.AddTransient<MonthPickerViewModel>();
        builder.Services.AddTransient<PartialPaymentViewModel>();
        builder.Services.AddTransient<PaymentSourcesViewModel>();
        builder.Services.AddTransient<PaymentSourceEditViewModel>();

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<AddEntryPage>();
        builder.Services.AddTransient<CategoriesPage>();
        builder.Services.AddTransient<CategoryEditPopup>();
        builder.Services.AddTransient<MonthPickerPopup>();
        builder.Services.AddTransient<PartialPaymentPopup>();
        builder.Services.AddTransient<PaymentSourcesPage>();
        builder.Services.AddTransient<PaymentSourceEditPopup>();

        return builder.Build();
    }
}