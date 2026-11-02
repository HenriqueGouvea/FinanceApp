using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Services;

namespace FinanceApp.ViewModels;

/// <summary>
/// The user's name and photo: the greeting at the top of the hamburger menu and the "Usuário" section of the settings.
/// </summary>
public partial class UserProfileViewModel : ObservableObject,
    IRecipient<SetupCompletedMessage>,
    IRecipient<ProfileChangedMessage>
{
    private readonly IUserProfileService _profileService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Greeting), nameof(Initial), nameof(DisplayName))]
    string name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    string? photoPath;

    public string Greeting => string.IsNullOrWhiteSpace(Name) ? "Olá!" : $"Olá, {Name}!";
    public string Initial => string.IsNullOrWhiteSpace(Name) ? "👤" : Name[..1].ToUpperInvariant();
    public bool HasPhoto => PhotoPath is not null;

    /// <summary>
    /// The name, or an invitation to add one, as shown in the settings.
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Adicionar nome e foto" : Name;

    public UserProfileViewModel(IUserProfileService profileService, IMessenger messenger)
    {
        _profileService = profileService;
        messenger.RegisterAll(this);
    }

    public void Receive(SetupCompletedMessage message) => Load();
    public void Receive(ProfileChangedMessage message) => Load();

    [RelayCommand]
    public void Load()
    {
        Name = _profileService.Name;
        PhotoPath = _profileService.PhotoPath;
    }
}
