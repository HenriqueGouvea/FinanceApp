using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Services;

namespace FinanceApp.ViewModels;

/// <summary>
/// Edits the user's name and photo, from the "Usuário" section of the settings.
/// </summary>
public partial class ProfileEditViewModel : ObservableObject
{
    private readonly IUserProfileService _profileService;
    private readonly IMediaPicker _mediaPicker;
    private readonly IMessenger _messenger;
    private Func<Task>? _onSaved;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Initial))]
    string name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    string? photoPath;

    public bool HasPhoto => PhotoPath is not null;
    public string Initial => string.IsNullOrWhiteSpace(Name) ? "👤" : Name.Trim()[..1].ToUpperInvariant();

    public ProfileEditViewModel(IUserProfileService profileService, IMediaPicker mediaPicker, IMessenger messenger)
    {
        _profileService = profileService;
        _mediaPicker = mediaPicker;
        _messenger = messenger;
    }

    public void Prepare()
    {
        Name = _profileService.Name;
        PhotoPath = _profileService.PhotoPath;
    }

    public void SetSaveCallback(Func<Task> onSaved)
    {
        _onSaved = onSaved;
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
    async Task Save()
    {
        await _profileService.SaveAsync(Name, PhotoPath);
        _messenger.Send(new ProfileChangedMessage());

        if (_onSaved is not null)
            await _onSaved();
    }
}
