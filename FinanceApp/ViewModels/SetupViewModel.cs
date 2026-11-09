using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Models;
using FinanceApp.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace FinanceApp.ViewModels;

/// <summary>
/// The setup wizard: name and photo, accounts and vouchers, credit cards and the purchases already on them, income,
/// house expenses and other expenses (health, children's school, the car, eating out), then a summary of the entries it will
/// create. Shown on the first run and again from the settings to add more.
/// </summary>
public partial class SetupViewModel : ObservableObject
{
    public const int NoDueDay = 0;
    public const int DefaultSalaryDay = 5;
    public const int DefaultAdvanceDay = 20;
    public const string SalaryCategory = "Salário";
    public const string SalaryDescription = "Salário";
    public const string AdvanceDescription = "Adiantamento salarial";
    public const string PurchaseIcon = "💳";
    public const int DefaultIptuStartMonth = 2;
    public const int DefaultIpvaStartMonth = 1;
    public const int DefaultLicensingMonth = 7;
    public const string DefaultHomeName = "Casa";
    public const string DefaultCarName = "Carro";
    public const string CarInsuranceDescription = "Seguro do carro";

    private const string SalaryIcon = "💼";

    private readonly ISetupService _setupService;
    private readonly IPaymentSourceService _paymentSourceService;
    private readonly IUserProfileService _profileService;
    private readonly IMediaPicker _mediaPicker;
    private readonly IMessenger _messenger;
    private readonly IWriteGuard _writeGuard;
    private readonly TimeProvider _timeProvider;
    private readonly HashSet<SetupStep> _skippedSteps = [];
    private Func<bool, Task>? _onClosed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepNumber), nameof(Progress), nameof(IsFirstStep), nameof(IsLastStep), nameof(NextButtonText),
        nameof(IsWelcomeStep), nameof(IsPaymentSourcesStep), nameof(IsCreditCardsStep), nameof(IsIncomeStep),
        nameof(IsHouseExpensesStep), nameof(IsOtherExpensesStep), nameof(IsSummaryStep))]
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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCardCommand))]
    string newCardName = string.Empty;

    [ObservableProperty] decimal newCardLimit;
    [ObservableProperty] int newCardBillDueDay = NoDueDay;
    [ObservableProperty] PaymentSource? newCardBillAccount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPurchaseCommand))]
    string newPurchaseDescription = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPurchaseCommand))]
    PaymentSource? newPurchaseCard;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNewPurchaseInstallments))]
    [NotifyCanExecuteChangedFor(nameof(AddPurchaseCommand))]
    RecurrenceType newPurchaseRecurrence = RecurrenceType.OneTime;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPurchaseCommand))]
    decimal newPurchaseAmount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPurchaseCommand))]
    int? newPurchaseCurrentInstallment;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPurchaseCommand))]
    int? newPurchaseTotalInstallments;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlySalary))]
    decimal salary;

    [ObservableProperty] int salaryDay = DefaultSalaryDay;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SalaryCaption), nameof(MonthlySalary))]
    bool isSalarySplit;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlySalary))]
    decimal advance;

    [ObservableProperty] int advanceDay = DefaultAdvanceDay;
    [ObservableProperty] PaymentSource? salarySource;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExpensesSourceCreditCard))]
    PaymentSource? expensesSource;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOtherExpensesSourceCreditCard))]
    PaymentSource? otherExpensesSource;

    [ObservableProperty] bool hasChildren;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCarInsurancePaymentVisible))]
    bool hasCar;

    /// <summary>
    /// The car insurance row of <see cref="CarExpenses"/>, whose payment is chosen below the list.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCarInsurancePaymentVisible))]
    SetupExpenseItem? carInsurance;
    [ObservableProperty] bool hasOwnHome;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddHomeCommand))]
    decimal newHomeFinancing;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddHomeCommand))]
    decimal newHomeIptu;

    [ObservableProperty] string newHomeName = string.Empty;
    [ObservableProperty] int newHomeFinancingDueDay = NoDueDay;
    [ObservableProperty] int newHomeIptuStartMonth = DefaultIptuStartMonth;
    [ObservableProperty] int newHomeIptuInstallments = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCarCommand))]
    decimal newCarIpva;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCarCommand))]
    decimal newCarLicensing;

    [ObservableProperty] string newCarName = string.Empty;
    [ObservableProperty] int newCarIpvaStartMonth = DefaultIpvaStartMonth;
    [ObservableProperty] int newCarIpvaInstallments = 1;
    [ObservableProperty] int newCarLicensingMonth = DefaultLicensingMonth;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSkippedSteps))]
    string skippedStepsText = string.Empty;

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
    public bool IsCreditCardsStep => Step == SetupStep.CreditCards;
    public bool IsIncomeStep => Step == SetupStep.Income;
    public bool IsHouseExpensesStep => Step == SetupStep.HouseExpenses;
    public bool IsOtherExpensesStep => Step == SetupStep.OtherExpenses;
    public bool IsSummaryStep => Step == SetupStep.Summary;
    public string NextButtonText => IsLastStep ? "Começar" : "Continuar";

    /// <summary>
    /// The first run can be skipped for good; run again from the settings, the wizard is just closed.
    /// </summary>
    public string SkipButtonText => IsRerun ? "Fechar" : "Pular tudo";

    public bool HasPhoto => PhotoPath is not null;
    public bool HasSkippedSteps => SkippedStepsText.Length > 0;
    public string Initial => string.IsNullOrWhiteSpace(Name) ? "👤" : Name.Trim()[..1].ToUpperInvariant();
    public string SummaryTitle => string.IsNullOrWhiteSpace(Name) ? "Tudo pronto!" : $"Tudo pronto, {Name.Trim()}!";
    public decimal Balance => TotalIncome - TotalOutcome;
    public bool HasNewSources => NewSources.Count > 0;
    public bool HasAvailableSources => AvailableSources.Count > 0;
    public bool HasBillAccounts => BillAccounts.Count > 0;
    public bool HasNewCards => NewCards.Count > 0;
    public bool HasCreditCards => CreditCards.Count > 0;
    public bool HasPurchases => Purchases.Count > 0;
    public bool HasSummaryRows => SummaryRows.Count > 0;
    public bool IsNewPurchaseInstallments => NewPurchaseRecurrence == RecurrenceType.Installments;
    public bool IsCarInsurancePaymentVisible => HasCar && CarInsurance is { IsSelected: true };

    /// <summary>
    /// Split in two, the salary field holds what is left once the advance is paid.
    /// </summary>
    public string SalaryCaption => IsSalarySplit ? "Pagamento (restante do salário)" : "Salário";

    public decimal MonthlySalary => Salary + (IsSalarySplit ? Advance : 0);

    // Expenses paid with a credit card go to its bill, which is never estimated nor paid little by little.
    public bool IsExpensesSourceCreditCard => ExpensesSource?.Type == PaymentSourceType.CreditCard;
    public bool IsOtherExpensesSourceCreditCard => OtherExpensesSource?.Type == PaymentSourceType.CreditCard;

    /// <summary>
    /// Accounts and vouchers added in this run, created only when the wizard finishes.
    /// </summary>
    public ObservableCollection<PaymentSource> NewSources { get; } = [];

    /// <summary>
    /// Credit cards added in this run, created only when the wizard finishes.
    /// </summary>
    public ObservableCollection<SetupCreditCardItem> NewCards { get; } = [];

    /// <summary>
    /// Sources offered to receive the salary and pay the expenses: the existing ones followed by the new ones.
    /// </summary>
    public ObservableCollection<PaymentSource> AvailableSources { get; } = [];

    /// <summary>
    /// Bank accounts offered to pay a new card's bill: the existing ones followed by the new ones.
    /// </summary>
    public ObservableCollection<PaymentSource> BillAccounts { get; } = [];

    /// <summary>
    /// Cards offered for a purchase: the existing ones followed by the new ones.
    /// </summary>
    public ObservableCollection<PaymentSource> CreditCards { get; } = [];

    public ObservableCollection<SetupPurchaseItem> Purchases { get; } = [];
    public ObservableCollection<SetupExpenseItem> Expenses { get; } = [];
    public ObservableCollection<SetupExpenseItem> HealthExpenses { get; } = [];
    public ObservableCollection<SetupExpenseItem> SchoolExpenses { get; } = [];
    public ObservableCollection<SetupExpenseItem> LeisureExpenses { get; } = [];
    public ObservableCollection<SetupExpenseItem> CarExpenses { get; } = [];
    public ObservableCollection<SetupHomeItem> Homes { get; } = [];
    public ObservableCollection<SetupCarItem> Cars { get; } = [];
    public bool HasHomes => Homes.Count > 0;
    public bool HasCars => Cars.Count > 0;
    public List<int> Months { get; } = [.. Enumerable.Range(1, 12)];

    /// <summary>
    /// Installments of a yearly tax: 1 is a single payment ("cota única").
    /// </summary>
    public List<int> AnnualInstallmentCounts { get; } = [.. Enumerable.Range(1, 12)];

    /// <summary>
    /// How an expense that offers it (the car insurance) is paid: every month, once a year ("À vista") or in 2 to 12
    /// installments a year.
    /// </summary>
    public List<int> YearlyPaymentOptions { get; } = [SetupExpenseItem.MonthlyPayment, .. Enumerable.Range(1, 12)];
    public ObservableCollection<SetupSummaryRow> SummaryRows { get; } = [];
    public List<int> DueDays { get; } = [NoDueDay, .. Enumerable.Range(1, 31)];
    public List<RecurrenceType> PurchaseRecurrences { get; } = [RecurrenceType.OneTime, RecurrenceType.Installments, RecurrenceType.Recurrent];

    public SetupViewModel(
        ISetupService setupService,
        IPaymentSourceService paymentSourceService,
        IUserProfileService profileService,
        IMediaPicker mediaPicker,
        IMessenger messenger,
        IWriteGuard writeGuard,
        TimeProvider timeProvider)
    {
        _setupService = setupService;
        _paymentSourceService = paymentSourceService;
        _profileService = profileService;
        _mediaPicker = mediaPicker;
        _messenger = messenger;
        _writeGuard = writeGuard;
        _timeProvider = timeProvider;
        NewSources.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNewSources));
        NewCards.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNewCards));
        AvailableSources.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAvailableSources));
        BillAccounts.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasBillAccounts));
        CreditCards.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasCreditCards));
        Purchases.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasPurchases));
        SummaryRows.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSummaryRows));
        Homes.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasHomes));
        Cars.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasCars));
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
        NewCards.Clear();
        AvailableSources.Clear();
        BillAccounts.Clear();
        CreditCards.Clear();
        Purchases.Clear();
        foreach (var source in await _paymentSourceService.GetAllAsync())
            AddToPickers(source);
        ResetNewSourceForm();
        ResetNewCardForm();
        ResetNewPurchaseForm();
        NewCardBillAccount = null;
        NewPurchaseCard = CreditCards.FirstOrDefault();

        Salary = 0;
        SalaryDay = DefaultSalaryDay;
        IsSalarySplit = false;
        Advance = 0;
        AdvanceDay = DefaultAdvanceDay;
        SalarySource = null;
        ExpensesSource = null;
        OtherExpensesSource = null;
        HasChildren = false;
        HasCar = false;
        HasOwnHome = false;
        Homes.Clear();
        Cars.Clear();
        ResetNewHomeForm();
        ResetNewCarForm();
        _skippedSteps.Clear();
        SkippedStepsText = string.Empty;

        Reset(Expenses, CreateHouseExpenses());
        Reset(HealthExpenses, CreateHealthExpenses());
        Reset(SchoolExpenses, CreateSchoolExpenses());
        Reset(LeisureExpenses, CreateLeisureExpenses());
        ResetCarExpenses();

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

    // --- Accounts and vouchers ---

    [RelayCommand]
    void SelectSourceType(PaymentSourceType value) => NewSourceType = value;

    private bool CanAddSource() => !string.IsNullOrWhiteSpace(NewSourceName);

    /// <summary>
    /// The first bank account added becomes where the salary is received, the expenses are paid and the new cards'
    /// bills are debited.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddSource))]
    void AddSource()
    {
        var source = new PaymentSource { Name = NewSourceName.Trim(), Type = NewSourceType, Balance = NewSourceBalance };
        NewSources.Add(source);
        AddToPickers(source);

        if (source.Type == PaymentSourceType.BankAccount)
        {
            SalarySource ??= source;
            ExpensesSource ??= source;
            OtherExpensesSource ??= source;
            NewCardBillAccount ??= source;
        }

        ResetNewSourceForm();
    }

    [RelayCommand]
    void RemoveSource(PaymentSource source)
    {
        NewSources.Remove(source);
        RemoveFromPickers(source);

        if (NewCardBillAccount == source)
            NewCardBillAccount = null;
        foreach (var card in NewCards.Where(c => c.BillAccount == source))
            card.BillAccount = null;
    }

    // --- Credit cards ---

    private bool CanAddCard() => !string.IsNullOrWhiteSpace(NewCardName);

    [RelayCommand(CanExecute = nameof(CanAddCard))]
    void AddCard()
    {
        var card = new PaymentSource
        {
            Name = NewCardName.Trim(),
            Type = PaymentSourceType.CreditCard,
            Balance = NewCardLimit,
            BillDueDay = NewCardBillDueDay == NoDueDay ? null : NewCardBillDueDay
        };
        NewCards.Add(new SetupCreditCardItem(card, NewCardBillAccount));
        AddToPickers(card);
        NewPurchaseCard ??= card;

        ResetNewCardForm();
    }

    [RelayCommand]
    void RemoveCard(SetupCreditCardItem item)
    {
        NewCards.Remove(item);
        RemoveFromPickers(item.Card);

        foreach (var purchase in Purchases.Where(p => p.Card == item.Card).ToList())
            Purchases.Remove(purchase);
        if (NewPurchaseCard == item.Card)
            NewPurchaseCard = CreditCards.FirstOrDefault();
    }

    [RelayCommand]
    void SelectPurchaseRecurrence(RecurrenceType value) => NewPurchaseRecurrence = value;

    private bool CanAddPurchase() =>
        !string.IsNullOrWhiteSpace(NewPurchaseDescription)
        && NewPurchaseCard is not null
        && NewPurchaseAmount > 0
        && (!IsNewPurchaseInstallments
            || NewPurchaseTotalInstallments is > 0 && NewPurchaseCurrentInstallment is > 0
            && NewPurchaseCurrentInstallment <= NewPurchaseTotalInstallments);

    [RelayCommand(CanExecute = nameof(CanAddPurchase))]
    void AddPurchase()
    {
        Purchases.Add(new SetupPurchaseItem(
            NewPurchaseDescription.Trim(),
            NewPurchaseAmount,
            NewPurchaseRecurrence,
            NewPurchaseCard!,
            IsNewPurchaseInstallments ? NewPurchaseTotalInstallments : null,
            IsNewPurchaseInstallments ? NewPurchaseCurrentInstallment : null));

        ResetNewPurchaseForm();
    }

    [RelayCommand]
    void RemovePurchase(SetupPurchaseItem purchase) => Purchases.Remove(purchase);

    // --- Expenses ---

    [RelayCommand]
    void ToggleExpense(SetupExpenseItem expense) => expense.IsSelected = !expense.IsSelected;

    [RelayCommand]
    void SelectHasChildren(bool value) => HasChildren = value;

    [RelayCommand]
    void SelectHasCar(bool value) => HasCar = value;

    [RelayCommand]
    void SelectHasOwnHome(bool value) => HasOwnHome = value;

    private bool CanAddHome() => NewHomeFinancing > 0 || NewHomeIptu > 0;

    /// <summary>
    /// Adds a home the user owns, with its monthly financing and its IPTU. An unnamed one is called "Casa".
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddHome))]
    void AddHome()
    {
        Homes.Add(new SetupHomeItem(
            string.IsNullOrWhiteSpace(NewHomeName) ? DefaultHomeName : NewHomeName.Trim(),
            NewHomeFinancing,
            NewHomeFinancingDueDay,
            NewHomeIptu,
            NewHomeIptuStartMonth,
            NewHomeIptuInstallments));
        ResetNewHomeForm();
    }

    [RelayCommand]
    void RemoveHome(SetupHomeItem home) => Homes.Remove(home);

    private bool CanAddCar() => NewCarIpva > 0 || NewCarLicensing > 0;

    /// <summary>
    /// Adds a car with its IPVA and licensing fee. An unnamed one is called "Carro".
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddCar))]
    void AddCar()
    {
        Cars.Add(new SetupCarItem(
            string.IsNullOrWhiteSpace(NewCarName) ? DefaultCarName : NewCarName.Trim(),
            NewCarIpva,
            NewCarIpvaStartMonth,
            NewCarIpvaInstallments,
            NewCarLicensing,
            NewCarLicensingMonth));
        ResetNewCarForm();
    }

    [RelayCommand]
    void RemoveCar(SetupCarItem car) => Cars.Remove(car);

    // --- Navigation ---

    [RelayCommand]
    async Task Next()
    {
        if (IsLastStep)
        {
            await FinishAsync();
            return;
        }

        // What was typed in a form but not added yet would otherwise be lost: "Continuar" reads as confirming it.
        if (IsPaymentSourcesStep && CanAddSource())
            AddSource();
        if (IsCreditCardsStep && CanAddCard())
            AddCard();
        if (IsCreditCardsStep && CanAddPurchase())
            AddPurchase();
        if (IsHouseExpensesStep && HasOwnHome && CanAddHome())
            AddHome();
        if (IsOtherExpensesStep && HasCar && CanAddCar())
            AddCar();

        _skippedSteps.Remove(Step);
        GoForward();
    }

    /// <summary>
    /// Discards what was filled in on the current step and moves on. The summary lists the skipped steps, which can
    /// be filled in by going back or later from the settings.
    /// </summary>
    [RelayCommand]
    void SkipStep()
    {
        if (IsLastStep)
            return;

        ClearStep(Step);
        _skippedSteps.Add(Step);
        GoForward();
    }

    private void GoForward()
    {
        Step++;
        if (IsSummaryStep)
            BuildSummary();
    }

    private void ClearStep(SetupStep step)
    {
        switch (step)
        {
            case SetupStep.Welcome:
                Name = _profileService.Name;
                PhotoPath = _profileService.PhotoPath;
                break;
            case SetupStep.PaymentSources:
                foreach (var source in NewSources.ToList())
                    RemoveSource(source);
                ResetNewSourceForm();
                break;
            case SetupStep.CreditCards:
                foreach (var card in NewCards.ToList())
                    RemoveCard(card);
                ResetNewCardForm();
                ResetNewPurchaseForm();
                break;
            case SetupStep.Income:
                Salary = 0;
                SalaryDay = DefaultSalaryDay;
                IsSalarySplit = false;
                Advance = 0;
                AdvanceDay = DefaultAdvanceDay;
                break;
            case SetupStep.HouseExpenses:
                Reset(Expenses, CreateHouseExpenses());
                HasOwnHome = false;
                Homes.Clear();
                ResetNewHomeForm();
                break;
            case SetupStep.OtherExpenses:
                HasChildren = false;
                HasCar = false;
                Cars.Clear();
                ResetNewCarForm();
                Reset(HealthExpenses, CreateHealthExpenses());
                Reset(SchoolExpenses, CreateSchoolExpenses());
                ResetCarExpenses();
                Reset(LeisureExpenses, CreateLeisureExpenses());
                break;
        }
    }

    /// <summary>
    /// "Nome e foto", "Cartões de crédito"...: how the summary names a skipped step.
    /// </summary>
    public static string DescribeStep(SetupStep step) => step switch
    {
        SetupStep.Welcome => "Nome e foto",
        SetupStep.PaymentSources => "Contas e vales",
        SetupStep.CreditCards => "Cartões de crédito",
        SetupStep.Income => "Renda",
        SetupStep.HouseExpenses => "Gastos da casa",
        SetupStep.OtherExpenses => "Outros gastos",
        _ => string.Empty
    };

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
        if (!await _writeGuard.TryAsync(() => _setupService.CompleteAsync(BuildSetup())))
            return;

        _messenger.Send(new SetupCompletedMessage());

        if (_onClosed is not null)
            await _onClosed(true);
    }

    private NewUserSetup BuildSetup() => new()
    {
        Name = Name,
        PhotoPath = PhotoPath,
        PaymentSources = [.. NewSources],
        CreditCards = [.. NewCards.Select(c => new SetupCreditCard { Card = c.Card, BillAccount = c.BillAccount })],
        Entries = [.. BuildEntries().Select(e => e.Entry)]
    };

    private IEnumerable<(string Icon, SetupEntry Entry)> BuildEntries()
    {
        if (Salary > 0)
            yield return (SalaryIcon, CreateIncome(SalaryDescription, Salary, SalaryDay));

        if (IsSalarySplit && Advance > 0)
            yield return (SalaryIcon, CreateIncome(AdvanceDescription, Advance, AdvanceDay));

        foreach (var purchase in Purchases)
            yield return (PurchaseIcon, new SetupEntry
            {
                Description = purchase.Description,
                Amount = purchase.Amount,
                Recurrence = purchase.Recurrence,
                TotalInstallments = purchase.TotalInstallments,
                CurrentInstallment = purchase.CurrentInstallment,
                PaymentSource = purchase.Card
            });

        foreach (var entry in BuildExpenses(Expenses, ExpensesSource))
            yield return entry;

        if (HasOwnHome)
            foreach (var entry in BuildHomeEntries())
                yield return entry;

        IEnumerable<SetupExpenseItem> otherExpenses =
        [
            .. HealthExpenses,
            .. HasChildren ? SchoolExpenses : Enumerable.Empty<SetupExpenseItem>(),
            .. HasCar ? CarExpenses : Enumerable.Empty<SetupExpenseItem>()
        ];
        foreach (var entry in BuildExpenses(otherExpenses, OtherExpensesSource))
            yield return entry;

        if (HasCar)
            foreach (var entry in BuildCarEntries())
                yield return entry;

        foreach (var entry in BuildExpenses(LeisureExpenses, OtherExpensesSource))
            yield return entry;
    }

    private IEnumerable<(string Icon, SetupEntry Entry)> BuildHomeEntries()
    {
        foreach (var home in Homes)
        {
            if (home.Financing > 0)
                yield return ("🏠", new SetupEntry
                {
                    Description = $"Financiamento ({home.Name})",
                    Amount = home.Financing,
                    DueDay = home.FinancingDueDay == NoDueDay ? null : home.FinancingDueDay,
                    CategoryName = "Moradia",
                    PaymentSource = ExpensesSource
                });

            if (home.Iptu > 0)
                yield return ("🧾", CreateAnnualTax($"IPTU ({home.Name})", home.Iptu, home.IptuStartMonth, home.IptuInstallments, "Moradia", ExpensesSource));
        }
    }

    private IEnumerable<(string Icon, SetupEntry Entry)> BuildCarEntries()
    {
        foreach (var car in Cars)
        {
            if (car.Ipva > 0)
                yield return ("🚗", CreateAnnualTax($"IPVA ({car.Name})", car.Ipva, car.IpvaStartMonth, car.IpvaInstallments, "Transporte", OtherExpensesSource));

            if (car.Licensing > 0)
                yield return ("📄", CreateAnnualTax($"Licenciamento ({car.Name})", car.Licensing, car.LicensingMonth, 1, "Transporte", OtherExpensesSource));
        }
    }

    private static SetupEntry CreateAnnualTax(string description, decimal amount, int startMonth, int installments, string category, PaymentSource? source) => new()
    {
        Description = description,
        Amount = amount,
        Recurrence = RecurrenceType.Annual,
        AnnualStartMonth = startMonth,
        TotalInstallments = installments,
        CategoryName = category,
        PaymentSource = source
    };

    private SetupEntry CreateIncome(string description, decimal amount, int day) => new()
    {
        Description = description,
        Amount = amount,
        EntryType = FinancialEntryType.Income,
        DueDay = day == NoDueDay ? null : day,
        CategoryName = SalaryCategory,
        PaymentSource = SalarySource
    };

    private static IEnumerable<(string Icon, SetupEntry Entry)> BuildExpenses(IEnumerable<SetupExpenseItem> expenses, PaymentSource? source)
    {
        var isOnCardBill = source?.Type == PaymentSourceType.CreditCard;

        return expenses.Where(e => e.IsSelected && e.Amount > 0).Select(expense => (expense.Icon, new SetupEntry
        {
            Description = expense.Description,
            Amount = expense.Amount,
            Recurrence = expense.IsPaidYearly ? RecurrenceType.Annual : RecurrenceType.Recurrent,
            TotalInstallments = expense.IsPaidYearly ? expense.YearlyInstallments : null,
            AnnualStartMonth = expense.IsPaidYearly ? expense.YearlyStartMonth : null,
            DueDay = expense.DueDay == NoDueDay ? null : expense.DueDay,
            IsEstimated = expense.IsEstimated && !isOnCardBill,
            AllowsPartialPayment = expense.AllowsPartialPayment && !isOnCardBill,
            CategoryName = expense.CategoryName,
            PaymentSource = source
        }));
    }

    private void BuildSummary()
    {
        var entries = BuildEntries().ToList();

        SummaryRows.Clear();
        foreach (var (icon, entry) in entries)
            SummaryRows.Add(new SetupSummaryRow(icon, entry.Description, DescribeSchedule(entry), entry.Amount, entry.EntryType));

        TotalIncome = entries.Where(e => e.Entry.EntryType == FinancialEntryType.Income).Sum(e => e.Entry.Amount);
        TotalOutcome = entries.Where(e => e.Entry.EntryType == FinancialEntryType.Outcome).Sum(e => e.Entry.Amount);

        SkippedStepsText = string.Join(", ", Enum.GetValues<SetupStep>().Where(_skippedSteps.Contains).Select(DescribeStep));
    }

    /// <summary>
    /// "À vista", "Parcela 3 de 10", "Todo ano, janeiro a março (3x)" or "Todo mês".
    /// </summary>
    public static string DescribeRecurrence(
        RecurrenceType recurrence, int? totalInstallments, int? currentInstallment, int? annualStartMonth = null) => recurrence switch
    {
        RecurrenceType.OneTime => "À vista",
        RecurrenceType.Installments => $"Parcela {currentInstallment ?? 1} de {totalInstallments}",
        RecurrenceType.Annual => DescribeAnnual(annualStartMonth ?? 1, totalInstallments ?? 1),
        _ => "Todo mês"
    };

    /// <summary>
    /// "Todo ano em julho", or "Todo ano, janeiro a março (3x)" when paid in installments.
    /// </summary>
    public static string DescribeAnnual(int startMonth, int installments)
    {
        if (installments <= 1)
            return $"Todo ano em {MonthName(startMonth)}";

        var endMonth = (startMonth - 1 + installments - 1) % 12 + 1;
        return $"Todo ano, {MonthName(startMonth)} a {MonthName(endMonth)} ({installments}x)";
    }

    private static string MonthName(int month) => PtBr.DateTimeFormat.GetMonthName(month);

    private static readonly CultureInfo PtBr = new("pt-BR");

    // A credit card outcome falls due with the card's bill, so its own due day isn't shown.
    private static string DescribeSchedule(SetupEntry entry)
    {
        var isOnCardBill = entry.EntryType == FinancialEntryType.Outcome && entry.PaymentSource?.Type == PaymentSourceType.CreditCard;

        List<string> parts = [DescribeRecurrence(entry.Recurrence, entry.TotalInstallments, entry.CurrentInstallment, entry.AnnualStartMonth)];
        if (entry.DueDay is { } day && !isOnCardBill)
            parts.Add($"dia {day}");
        if (entry.IsEstimated)
            parts.Add("valor estimado");
        if (entry.AllowsPartialPayment)
            parts.Add("pagamento parcial");
        if (entry.PaymentSource is { } source)
            parts.Add(source.Name);
        return string.Join(" · ", parts);
    }

    private void AddToPickers(PaymentSource source)
    {
        AvailableSources.Add(source);
        if (source.Type == PaymentSourceType.BankAccount)
            BillAccounts.Add(source);
        if (source.Type == PaymentSourceType.CreditCard)
            CreditCards.Add(source);
    }

    private void RemoveFromPickers(PaymentSource source)
    {
        AvailableSources.Remove(source);
        BillAccounts.Remove(source);
        CreditCards.Remove(source);

        if (SalarySource == source)
            SalarySource = null;
        if (ExpensesSource == source)
            ExpensesSource = null;
        if (OtherExpensesSource == source)
            OtherExpensesSource = null;
    }

    private void ResetNewSourceForm()
    {
        NewSourceName = string.Empty;
        NewSourceType = PaymentSourceType.BankAccount;
        NewSourceBalance = 0;
    }

    // The bill account is kept for the next card: people usually pay every card from the same account.
    private void ResetNewCardForm()
    {
        NewCardName = string.Empty;
        NewCardLimit = 0;
        NewCardBillDueDay = NoDueDay;
    }

    private void ResetNewHomeForm()
    {
        NewHomeName = string.Empty;
        NewHomeFinancing = 0;
        NewHomeFinancingDueDay = NoDueDay;
        NewHomeIptu = 0;
        NewHomeIptuStartMonth = DefaultIptuStartMonth;
        NewHomeIptuInstallments = 1;
    }

    private void ResetNewCarForm()
    {
        NewCarName = string.Empty;
        NewCarIpva = 0;
        NewCarIpvaStartMonth = DefaultIpvaStartMonth;
        NewCarIpvaInstallments = 1;
        NewCarLicensing = 0;
        NewCarLicensingMonth = DefaultLicensingMonth;
    }

    // The card is kept for the next purchase, which is usually on the same card.
    private void ResetNewPurchaseForm()
    {
        NewPurchaseDescription = string.Empty;
        NewPurchaseRecurrence = RecurrenceType.OneTime;
        NewPurchaseAmount = 0;
        NewPurchaseCurrentInstallment = null;
        NewPurchaseTotalInstallments = null;
    }

    private void ResetCarExpenses()
    {
        Reset(CarExpenses, CreateCarExpenses());
        CarInsurance = CarExpenses.Single(e => e.OffersYearlyPayment);
    }

    partial void OnCarInsuranceChanged(SetupExpenseItem? oldValue, SetupExpenseItem? newValue)
    {
        if (oldValue is not null)
            oldValue.PropertyChanged -= OnCarInsurancePropertyChanged;
        if (newValue is not null)
            newValue.PropertyChanged += OnCarInsurancePropertyChanged;
    }

    private void OnCarInsurancePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SetupExpenseItem.IsSelected))
            OnPropertyChanged(nameof(IsCarInsurancePaymentVisible));
    }

    private static void Reset(ObservableCollection<SetupExpenseItem> list, IEnumerable<SetupExpenseItem> items)
    {
        list.Clear();
        foreach (var item in items)
            list.Add(item);
    }

    private static IEnumerable<SetupExpenseItem> CreateHouseExpenses() =>
    [
        new() { Icon = "🏠", Description = "Aluguel", CategoryName = "Moradia" },
        new() { Icon = "🏢", Description = "Condomínio", CategoryName = "Moradia", CanBeEstimated = true },
        new() { Icon = "💡", Description = "Energia", CategoryName = "Moradia", CanBeEstimated = true, IsEstimated = true },
        new() { Icon = "🚿", Description = "Água", CategoryName = "Moradia", CanBeEstimated = true, IsEstimated = true },
        new() { Icon = "🌐", Description = "Internet", CategoryName = "Moradia" },
        new() { Icon = "📱", Description = "Celular", CategoryName = "Moradia" },
        new()
        {
            Icon = "🛒", Description = "Mercado", CategoryName = "Mercado",
            CanBeEstimated = true, IsEstimated = true, CanBePartiallyPaid = true, AllowsPartialPayment = true
        }
    ];

    // A health plan with copayment ("coparticipação") varies with the month's appointments.
    private static IEnumerable<SetupExpenseItem> CreateHealthExpenses() =>
    [
        new() { Icon = "🩺", Description = "Plano de saúde", CategoryName = "Saúde", CanBeEstimated = true },
        new()
        {
            Icon = "💊", Description = "Farmácia", CategoryName = "Saúde",
            CanBeEstimated = true, IsEstimated = true, CanBePartiallyPaid = true, AllowsPartialPayment = true
        }
    ];

    private static IEnumerable<SetupExpenseItem> CreateSchoolExpenses() =>
    [
        new() { Icon = "🎒", Description = "Mensalidade escolar", CategoryName = "Educação" },
        new() { Icon = "🚌", Description = "Transporte escolar", CategoryName = "Educação" },
        new() { Icon = "⚽", Description = "Cursos e atividades", CategoryName = "Educação" }
    ];

    private static IEnumerable<SetupExpenseItem> CreateLeisureExpenses() =>
    [
        new()
        {
            Icon = "🍽️", Description = "Restaurantes e delivery", CategoryName = "Lazer",
            CanBeEstimated = true, IsEstimated = true, CanBePartiallyPaid = true, AllowsPartialPayment = true
        }
    ];

    // The insurance policy lasts a year: paid every month, or at once or in installments from the month it renews
    // (the current month until the user picks another).
    private IEnumerable<SetupExpenseItem> CreateCarExpenses() =>
    [
        new()
        {
            Icon = "⛽", Description = "Combustível", CategoryName = "Transporte",
            CanBeEstimated = true, IsEstimated = true, CanBePartiallyPaid = true
        },
        new()
        {
            Icon = "🛡️", Description = CarInsuranceDescription, CategoryName = "Transporte",
            OffersYearlyPayment = true, YearlyStartMonth = _timeProvider.GetLocalNow().Month
        }
    ];
}
