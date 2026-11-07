using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Services;

namespace FinanceApp.ViewModels;

/// <summary>
/// The "Seus dados" section of the settings: "Exportar dados", "Importar backup" and "Apagar todos os dados".
/// </summary>
public partial class DataManagementViewModel : ObservableObject
{
    public const string DeleteTitle = "Apagar todos os dados";
    public const string DeleteMessage =
        "Isto apaga deste aparelho:\n" +
        "• os lançamentos e pagamentos\n" +
        "• as contas e cartões\n" +
        "• as categorias (as padrão voltam)\n" +
        "• seu nome e foto\n" +
        "• o PIN e as configurações\n\n" +
        "Não dá para desfazer. Para guardar uma cópia, use \"Exportar dados\" antes.";
    public const string DeleteAccept = "Apagar tudo";
    public const string ImportTitle = "Importar backup";
    public const string ImportAccept = "Importar";
    public const string Cancel = "Cancelar";
    public const string InvalidBackupTitle = "Arquivo inválido";
    public const string InvalidBackupMessage = "Este arquivo não é um backup do Mensal. Escolha o arquivo .json criado por \"Exportar dados\".";
    public const string NewerBackupMessage = "Este backup foi feito por uma versão mais nova do Mensal. Atualize o app e tente de novo.";

    private static readonly FilePickerFileType BackupFileType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        // Some apps (e.g. messengers) save downloaded files without a type, so those are offered too.
        [DevicePlatform.Android] = ["application/json", "application/octet-stream"],
        [DevicePlatform.iOS] = ["public.json"],
        [DevicePlatform.MacCatalyst] = ["public.json"],
        [DevicePlatform.WinUI] = [".json"]
    });

    private readonly IDataManagementService _dataService;
    private readonly IAlertService _alerts;
    private readonly IWriteGuard _writeGuard;
    private readonly IShare _share;
    private readonly IFilePicker _filePicker;
    private readonly IRecentsPrivacyService _recentsPrivacy;
    private readonly IMessenger _messenger;
    private Func<bool, Task>? _onDataReplaced;

    [ObservableProperty] bool isBusy;

    public DataManagementViewModel(
        IDataManagementService dataService,
        IAlertService alerts,
        IWriteGuard writeGuard,
        IShare share,
        IFilePicker filePicker,
        IRecentsPrivacyService recentsPrivacy,
        IMessenger messenger)
    {
        _dataService = dataService;
        _alerts = alerts;
        _writeGuard = writeGuard;
        _share = share;
        _filePicker = filePicker;
        _recentsPrivacy = recentsPrivacy;
        _messenger = messenger;
    }

    /// <summary>
    /// Called once the data was deleted (true) or replaced by a backup (false), so the page can leave the settings.
    /// </summary>
    public void SetDataReplacedCallback(Func<bool, Task> onDataReplaced) => _onDataReplaced = onDataReplaced;

    [RelayCommand]
    async Task ExportAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            IReadOnlyList<string> files = [];
            if (!await _writeGuard.TryAsync(async () => files = await _dataService.ExportAsync()))
                return;

            await _share.RequestAsync(new ShareMultipleFilesRequest
            {
                Title = "Exportar dados",
                Files = [.. files.Select(path => new ShareFile(path))]
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    async Task ImportAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var file = await _filePicker.PickAsync(new PickOptions { PickerTitle = ImportTitle, FileTypes = BackupFileType });
            if (file is null)
                return;

            var message =
                $"Os lançamentos, contas e cartões, categorias, nome e foto deste aparelho serão trocados pelos de \"{file.FileName}\". " +
                "O PIN continua o mesmo. Não dá para desfazer.";
            if (!await _alerts.ConfirmAsync(ImportTitle, message, ImportAccept, Cancel))
                return;

            InvalidBackupException? invalid = null;
            var imported = await _writeGuard.TryAsync(async () =>
            {
                try
                {
                    // The picker copies the chosen file into the cache, so FullPath is a real file on every platform.
                    await using var stream = File.OpenRead(file.FullPath);
                    await _dataService.ImportAsync(stream);
                }
                catch (InvalidBackupException ex)
                {
                    invalid = ex;
                }
            });

            if (invalid is not null)
            {
                await _alerts.ShowAsync(InvalidBackupTitle, invalid.IsFromNewerVersion ? NewerBackupMessage : InvalidBackupMessage);
                return;
            }
            if (!imported)
                return;

            await OnDataReplacedAsync(deleted: false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    async Task DeleteAllAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            if (!await _alerts.ConfirmAsync(DeleteTitle, DeleteMessage, DeleteAccept, Cancel))
                return;
            if (!await _writeGuard.TryAsync(_dataService.DeleteAllAsync))
                return;

            // The PIN is gone, so there is nothing left to hide.
            _recentsPrivacy.SetHidden(false);
            await OnDataReplacedAsync(deleted: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OnDataReplacedAsync(bool deleted)
    {
        _messenger.Send(new DataReplacedMessage());
        if (_onDataReplaced is not null)
            await _onDataReplaced(deleted);
    }
}
