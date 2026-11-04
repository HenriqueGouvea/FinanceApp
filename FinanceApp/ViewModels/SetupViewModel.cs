using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Models;
using FinanceApp.Services;
using System.Collections.ObjectModel;

namespace FinanceApp.ViewModels;

/// <summary>
/// The setup wizard: name and photo, accounts and cards, salary and house expenses, then a summary of the
/// recurrent entries it will create. Shown on the first run and again from the settings to add more.
/// </summary>
public partial class SetupViewModel : ObservableObject
{
    public const int NoDueDay = 0;
    public const int DefaultSalaryDay = 5;
    public const string SalaryCategory = "Salário";

    private readonly ISetupService _setupService;
    private readonly IPaymentSourceService _paymentSourceService;
    private readonly IUserProfileService _profileService;
    private readonly IMediaPicker _mediaPicker;
    private readonly IMessenger _messenger;
    private Func<bool, Task>? _onClosed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepNumber), nameof(Progress), nameof(IsFirstStep), nameof(IsLastStep), nameof(NextButtonText),
        nameof(IsWelcomeStep), nameof(IsPaymentSourcesStep), nameof(IsIncomeStep), nameof(IsHouseExpensesStep), nameof(IsSummaryStep))]
    SetupStep step = SetupStep.Welcome;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SkipButtonText))]
    bool isRerun;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Initial), nameof(SummaryTitle))]
    string name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    string? photoPath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddSourceCommand))]
    string newSourceName = string.Empty;

    [ObservableProperty] PaymentSourceType newSourceType = PaymentSourceType.BankAccount;
    [ObservableProperty] decimal newSourceBalance;

    [ObservableProperty] decimal salary;
    [ObservableProperty] int salaryDay = DefaultSalaryDay;
    [ObservableProperty] PaymentSource? salarySource;
    [ObservableProperty] PaymentSource? expensesSource;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Balance))]
    decimal totalIncome;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Balance))]
    decimal totalOutcome;

    public int StepCount { get; } = Enum.GetValues<SetupStep>().Length;
    public int StepNumber => (int)Step + 1;
    public double Progress => (double)StepNumber / StepCount;
    public bool IsFirstStep => Step == SetupStep.Welcome;
    public bool IsLastStep => Step == SetupStep.Summary;
    public bool IsWelcomeStep => Step == SetupStep.Welcome;
    public bool IsPaymentSourcesStep => Step == SetupStep.PaymentSources;
    public bool IsIncomeStep => Step == SetupStep.Income;
    public bool IsHouseExpensesStep => Step == SetupStep.HouseExpenses;
    public bool IsSummaryStep => Step == SetupStep.Summary;
    public string NextButtonText => IsLastStep ? "Começar" : "Continuar";

    /// <summary>
    /// The first run can be skipped for good; run again from the settings, the wizard is just closed.
    /// </summary>
    public string SkipButtonText => IsRerun ? "Fechar" : "Pular";

    public bool HasPhoto => PhotoPath is not null;
    public string Initial => string.IsNullOrWhiteSpace(Name) ? "👤" : Name.Trim()[..1].ToUpperInvariant();
    public string SummaryTitle => string.IsNullOrWhiteSpace(Name) ? "Tudo pronto!" : $"Tudo pronto, {Name.Trim()}!";
    public decimal Balance => TotalIncome - TotalOutcome;
    public bool HasNewSources => NewSources.Count > 0;
    public bool HasAvailableSources => AvailableSources.Count > 0;
    public bool HasSummaryRows => SummaryRows.Count > 0;

    /// <summary>
    /// Accounts, cards and vouchers added in this run, created only when the wizard finishes.
    /// </summary>
    public ObservableCollection<PaymentSource> NewSources { get; } = [];

    /// <summary>
    /// Sources offered to receive the salary and pay the expenses: the existing ones followed by the new ones.
    /// </summary>
    public ObservableCollection<PaymentSource> AvailableSources { get; } = [];

    public ObservableCollection<SetupExpenseItem> Expenses { get; } = [];
    public ObservableCollection<SetupSummaryRow> SummaryRows { get; } = [];
    public List<int> DueDays { get; } = [NoDueDay, .. Enumerable.Range(1, 31)];

    public SetupViewModel(
        ISetupService setupService,
        IPaymentSourceService paymentSourceService,
        IUserProfileService profileService,
        IMediaPicker mediaPicker,
        IMessenger messenger)
    {
        _setupService = setupService;
        _paymentSourceService = paymentSourceService;
        _profileService = profileService;
        _mediaPicker = mediaPicker;
        _messenger = messenger;
        NewSources.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNewSources));
        AvailableSources.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAvailableSources));
        SummaryRows.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSummaryRows));
    }

    /// <summary>
    /// Starts the wizard from its first step. Run again (<paramref name="isRerun"/>), it shows the current name and photo.
    /// </summary>
    public async Task PrepareAsync(bool isRerun)
    {
        IsRerun = isRerun;
        Step = SetupStep.Welcome;
        Name = _profileService.Name;
        PhotoPath = _profileService.PhotoPath;

        NewSources.Clear();
        AvailableSources.Clear();
        foreach (var source in await _paymentSourceService.GetAllAsync())
            AvailableSources.Add(source);
        ResetNewSourceForm();

        Salary = 0;
        SalaryDay = DefaultSalaryDay;
        SalarySource = null;
        ExpensesSource = null;

        Expenses.Clear();
        foreach (var expense in CreateExpenses())
            Expenses.Add(expense);

        SummaryRows.Clear();
        TotalIncome = 0;
        TotalOutcome = 0;
    }

    /// <summary>
    /// <paramref name="onClosed"/> runs once the wizard is finished (true) or skipped (false) and must close it.
    /// </summary>
    public void SetClosedCallback(Func<bool, Task> onClosed)
    {
        _onClosed = onClosed;
    }

    [RelayCommand]
    async Task PickPhoto()
    {
        if (await _mediaPicker.PickProfilePhotoAsync() is { } path)
            PhotoPath = path;
    }

    [RelayCommand]
    void RemovePhoto() => PhotoPath = null;

    [RelayCommand]
    void SelectSourceType(PaymentSourceType value) => NewSourceType = value;

    private bool CanAddSource() => !string.IsNullOrWhiteSpace(NewSourceName);

    /// <summary>
    /// The first bank account added becomes where the salary is received and the house expenses are paid.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddSource))]
    void AddSource()
    {
        var source = new PaymentSource { Name = NewSourceName.Trim(), Type = NewSourceType, Balance = NewSourceBalance };
        NewSources.Add(source);
        AvailableSources.Add(source);

        if (source.Type == PaymentSourceType.BankAccount)
        {
            SalarySource ??= source;
            ExpensesSource ??= source;
        }

        ResetNewSourceForm();
    }

    [RelayCommand]
    void RemoveSource(PaymentSource source)
    {
        NewSources.Remove(source);
        AvailableSources.Remove(source);

        if (SalarySource == source)
            SalarySource = null;
        if (ExpensesSource == source)
            ExpensesSource = null;
    }

    [RelayCommand]
    void ToggleExpense(SetupExpenseItem expense) => expense.IsSelected = !expense.IsSelected;

    [RelayCommand]
    async Task Next()
    {
        if (IsLastStep)
        {
            await FinishAsync();
            return;
        }

        // A source typed in the form but not added yet would otherwise be lost: "Continuar" reads as confirming it.
        if (IsPaymentSourcesStep && CanAddSource())
            AddSource();

        Step++;
        if (IsSummaryStep)
            BuildSummary();
    }

    [RelayCommand]
    void Back()
    {
        if (!IsFirstStep)
            Step--;
    }

    [RelayCommand]
    async Task Skip()
    {
        if (!IsRerun)
            _setupService.Skip();

        if (_onClosed is not null)
            await _onClosed(false);
    }

    private async Task FinishAsync()
    {
        await _setupService.CompleteAsync(BuildSetup());
        _messenger.Send(new SetupCompletedMessage());

        if (_onClosed is not null)
            await _onClosed(true);
    }

    private NewUserSetup BuildSetup() => new()
    {
        Name = Name,
        PhotoPath = PhotoPath,
        PaymentSources = [.. NewSources],
        Entries = [.. BuildEntries()]
    };

    private IEnumerable<SetupEntry> BuildEntries()
    {
        if (Salary > 0)
            yield return new SetupEntry
            {
                Description = SalaryCategory,
                Amount = Salary,
                EntryType = FinancialEntryType.Income,
                DueDay = SalaryDay == NoDueDay ? null : SalaryDay,
                CategoryName = SalaryCategory,
                PaymentSource = SalarySource
            };

        foreach (var expense in Expenses.Where(e => e.IsSelected && e.Amount > 0))
            yield return new SetupEntry
            {
                Description = expense.Description,
                Amount = expense.Amount,
                EntryType = FinancialEntryType.Outcome,
                DueDay = expense.DueDay == NoDueDay ? null : expense.DueDay,
                IsEstimated = expense.IsEstimated,
                AllowsPartialPayment = expense.AllowsPartialPayment,
                CategoryName = expense.CategoryName,
                PaymentSource = ExpensesSource
            };
    }

    private void BuildSummary()
    {
        var entries = BuildEntries().ToList();

        SummaryRows.Clear();
        foreach (var entry in entries)
            SummaryRows.Add(new SetupSummaryRow(
                entry.EntryType == FinancialEntryType.Income ? "💼" : Expenses.First(e => e.Description == entry.Description).Icon,
                entry.Description,
                DescribeSchedule(entry),
                entry.Amount,
                entry.EntryType));

        TotalIncome = entries.Where(e => e.EntryType == FinancialEntryType.Income).Sum(e => e.Amount);
        TotalOutcome = entries.Where(e => e.EntryType == FinancialEntryType.Outcome).Sum(e => e.Amount);
    }

    private static string DescribeSchedule(SetupEntry entry)
    {
        List<string> parts = ["Todo mês"];
        if (entry.DueDay is { } day)
            parts.Add($"dia {day}");
        if (entry.IsEstimated)
            parts.Add("valor estimado");
        if (entry.AllowsPartialPayment)
            parts.Add("pagamento parcial");
        if (entry.PaymentSource is { } source)
            parts.Add(source.Name);
        return string.Join(" · ", parts);
    }

    private void ResetNewSourceForm()
    {
        NewSourceName = string.Empty;
        NewSourceType = PaymentSourceType.BankAccount;
        NewSourceBalance = 0;
    }

    private static IEnumerable<SetupExpenseItem> CreateExpenses() =>
    [
        new() { Icon = "🏠", Description = "Aluguel ou financiamento", CategoryName = "Moradia" },
        new() { Icon = "🏢", Description = "Condomínio", CategoryName = "Moradia" },
        new() { Icon = "💡", Description = "Energia", CategoryName = "Moradia", IsEstimated = true },
        new() { Icon = "🚿", Description = "Água", CategoryName = "Moradia", IsEstimated = true },
        new() { Icon = "🌐", Description = "Internet", CategoryName = "Moradia" },
        new() { Icon = "📱", Description = "Celular", CategoryName = "Moradia" },
        new()
        {
            Icon = "🛒", Description = "Mercado", CategoryName = "Mercado",
            IsEstimated = true, AllowsPartialPayment = true
        }
    ];
}
