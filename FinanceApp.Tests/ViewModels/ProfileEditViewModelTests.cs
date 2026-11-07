using Microsoft.Extensions.Logging.Abstractions;
using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class ProfileEditViewModelTests
{
    private readonly IUserProfileService _profileService;
    private readonly IMediaPicker _mediaPicker;
    private readonly IMessenger _messenger = new WeakReferenceMessenger();
    private readonly ProfileEditViewModel _vm;

    public ProfileEditViewModelTests()
    {
        _profileService = Substitute.For<IUserProfileService>();
        _profileService.Name.Returns("Ana");
        _profileService.PhotoPath.Returns("/data/profile.jpg");
        _mediaPicker = Substitute.For<IMediaPicker>();
        _vm = new ProfileEditViewModel(_profileService, _mediaPicker, _messenger, new WriteGuard(Substitute.For<IAlertService>(), NullLogger<WriteGuard>.Instance));
    }

    [Fact]
    public void Prepare_ShowsCurrentProfile()
    {
        _vm.Prepare();

        _vm.Name.Should().Be("Ana");
        _vm.Initial.Should().Be("A");
        _vm.HasPhoto.Should().BeTrue();
    }

    [Fact]
    public async Task PickPhoto_Picked_ReplacesPhoto()
    {
        _vm.Prepare();
        _mediaPicker.PickPhotosAsync(Arg.Any<MediaPickerOptions>())
            .Returns(Task.FromResult(new List<FileResult> { new("/cache/new.jpg") }));

        await _vm.PickPhotoCommand.ExecuteAsync(null);

        _vm.PhotoPath.Should().Be("/cache/new.jpg");
    }

    [Fact]
    public async Task PickPhoto_PermissionDenied_KeepsPhoto()
    {
        _vm.Prepare();
        _mediaPicker.PickPhotosAsync(Arg.Any<MediaPickerOptions>())
            .Returns<Task<List<FileResult>>>(_ => throw new PermissionException("denied"));

        await _vm.PickPhotoCommand.ExecuteAsync(null);

        _vm.PhotoPath.Should().Be("/data/profile.jpg");
    }

    [Fact]
    public async Task Save_SavesProfileNotifiesAndCloses()
    {
        var closed = false;
        var received = false;
        _messenger.Register<ProfileChangedMessage>(this, (_, _) => received = true);
        _vm.Prepare();
        _vm.SetSaveCallback(() => { closed = true; return Task.CompletedTask; });
        _vm.Name = "Ana Maria";
        _vm.RemovePhotoCommand.Execute(null);

        await _vm.SaveCommand.ExecuteAsync(null);

        await _profileService.Received(1).SaveAsync("Ana Maria", null);
        received.Should().BeTrue();
        closed.Should().BeTrue();
    }
}
