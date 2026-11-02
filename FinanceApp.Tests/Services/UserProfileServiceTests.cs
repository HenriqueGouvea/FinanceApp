using FinanceApp.Services;
using FinanceApp.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace FinanceApp.Tests.Services;

public sealed class UserProfileServiceTests : IDisposable
{
    private readonly InMemoryPreferences _preferences = new();
    private readonly TempFileSystem _fileSystem = new();
    private readonly UserProfileService _service;

    public UserProfileServiceTests()
    {
        _service = new UserProfileService(_preferences, _fileSystem);
    }

    public void Dispose() => _fileSystem.Dispose();

    // --- Name ---

    [Fact]
    public void Name_NeverSaved_IsEmpty()
    {
        _service.Name.Should().BeEmpty();
        _service.PhotoPath.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_Name_IsTrimmed()
    {
        await _service.SaveAsync("  Ana  ", null);

        _service.Name.Should().Be("Ana");
    }

    // --- Photo ---

    [Fact]
    public async Task SaveAsync_NewPhoto_IsCopiedIntoAppData()
    {
        var picked = TempFileSystem.CreateExternalFile(".jpg", "photo");

        await _service.SaveAsync("Ana", picked);

        _service.PhotoPath.Should().StartWith(_fileSystem.AppDataDirectory).And.EndWith(".jpg");
        File.ReadAllText(_service.PhotoPath!).Should().Be("photo");
    }

    [Fact]
    public async Task SaveAsync_ReplacedPhoto_GetsNewFileAndDeletesOldOne()
    {
        await _service.SaveAsync("Ana", TempFileSystem.CreateExternalFile(".jpg", "first"));
        var firstPath = _service.PhotoPath!;

        await _service.SaveAsync("Ana", TempFileSystem.CreateExternalFile(".png", "second"));

        _service.PhotoPath.Should().NotBe(firstPath).And.EndWith(".png");
        File.ReadAllText(_service.PhotoPath!).Should().Be("second");
        File.Exists(firstPath).Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_CurrentPhoto_IsKept()
    {
        await _service.SaveAsync("Ana", TempFileSystem.CreateExternalFile(".jpg", "photo"));
        var path = _service.PhotoPath!;

        await _service.SaveAsync("Ana Maria", path);

        _service.PhotoPath.Should().Be(path);
        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_NullPhoto_RemovesCurrentPhoto()
    {
        await _service.SaveAsync("Ana", TempFileSystem.CreateExternalFile(".jpg", "photo"));
        var path = _service.PhotoPath!;

        await _service.SaveAsync("Ana", null);

        _service.PhotoPath.Should().BeNull();
        File.Exists(path).Should().BeFalse();
    }
}
