using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Services;

namespace FinanceApp.ViewModels;

/// <summary>
/// The greeting with the user's photo at the top of the hamburger menu.
/// </summary>
public partial class UserProfileViewModel : ObservableObject, IRecipient<SetupCompletedMessage>
{
    private readonly IUserProfileService _profileService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Greeting), nameof(Initial))]
    string name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    string? photoPath;

    public string Greeting => string.IsNullOrWhiteSpace(Name) ? "Olá!" : $"Olá, {Name}!";
    public string Initial => string.IsNullOrWhiteSpace(Name) ? "👤" : Name[..1].ToUpperInvariant();
    public bool HasPhoto => PhotoPath is not null;

    public UserProfileViewModel(IUserProfileService profileService, IMessenger messenger)
    {
        _profileService = profileService;
        messenger.Register(this);
    }

    public void Receive(SetupCompletedMessage message) => Load();

    [RelayCommand]
    public void Load()
    {
        Name = _profileService.Name;
        PhotoPath = _profileService.PhotoPath;
    }
}
