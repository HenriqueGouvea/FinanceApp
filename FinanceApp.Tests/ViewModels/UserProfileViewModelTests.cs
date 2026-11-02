using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Services;
using FinanceApp.ViewModels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class UserProfileViewModelTests
{
    private readonly IUserProfileService _profileService;
    private readonly IMessenger _messenger = new WeakReferenceMessenger();
    private readonly UserProfileViewModel _vm;

    public UserProfileViewModelTests()
    {
        _profileService = Substitute.For<IUserProfileService>();
        _profileService.Name.Returns(string.Empty);
        _profileService.PhotoPath.Returns((string?)null);
        _vm = new UserProfileViewModel(_profileService, _messenger);
    }

    [Fact]
    public void Load_NoProfile_GreetsWithoutName()
    {
        _vm.Load();

        _vm.Greeting.Should().Be("Olá!");
        _vm.Initial.Should().Be("👤");
        _vm.HasPhoto.Should().BeFalse();
    }

    [Fact]
    public void Load_Profile_GreetsByName()
    {
        _profileService.Name.Returns("ana");
        _profileService.PhotoPath.Returns("/data/profile.jpg");

        _vm.Load();

        _vm.Greeting.Should().Be("Olá, ana!");
        _vm.Initial.Should().Be("A");
        _vm.HasPhoto.Should().BeTrue();
    }

    [Fact]
    public void DisplayName_NoName_InvitesToAddOne()
    {
        _vm.Load();
        _vm.DisplayName.Should().Be("Adicionar nome e foto");

        _vm.Name = "Ana";
        _vm.DisplayName.Should().Be("Ana");
    }

    [Fact]
    public void ProfileChanged_ReloadsProfile()
    {
        _vm.Load();
        _profileService.Name.Returns("Ana");

        _messenger.Send(new ProfileChangedMessage());

        _vm.Greeting.Should().Be("Olá, Ana!");
    }

    [Fact]
    public void SetupCompleted_ReloadsProfile()
    {
        _vm.Load();
        _profileService.Name.Returns("Ana");

        _messenger.Send(new SetupCompletedMessage());

        _vm.Greeting.Should().Be("Olá, Ana!");
    }
}
