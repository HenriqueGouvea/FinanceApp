using Microsoft.Extensions.Logging;

namespace FinanceApp.Services;

public class WriteGuard : IWriteGuard
{
    public const string ErrorTitle = "Não foi possível salvar";
    public const string ErrorMessage = "Tente de novo.";

    private readonly IAlertService _alerts;
    private readonly ILogger<WriteGuard> _logger;

    public WriteGuard(IAlertService alerts, ILogger<WriteGuard> logger)
    {
        _alerts = alerts;
        _logger = logger;
    }

    public async Task<bool> TryAsync(Func<Task> write)
    {
        try
        {
            await write();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A write failed");
            await _alerts.ShowAsync(ErrorTitle, ErrorMessage);
            return false;
        }
    }
}
