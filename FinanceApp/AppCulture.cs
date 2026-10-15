using System.Globalization;

namespace FinanceApp;

public static class AppCulture
{
    public static readonly CultureInfo Default = new("pt-BR");

    public static void Apply()
    {
        CultureInfo.DefaultThreadCurrentCulture = Default;
        CultureInfo.DefaultThreadCurrentUICulture = Default;
        CultureInfo.CurrentCulture = Default;
        CultureInfo.CurrentUICulture = Default;
    }
}
