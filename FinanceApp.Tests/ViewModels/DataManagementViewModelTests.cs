using CommunityToolkit.Mvvm.Messaging;
using FinanceApp.Messages;
using FinanceApp.Services;
using FinanceApp.Tests.Infrastructure;
using FinanceApp.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FinanceApp.Tests.ViewModels;

public class DataManagementViewModelTests
{
    private readonly IDataManagementService _dataService = Substitute.For<IDataManagementService>();
    private readonly IAlertService _alerts = Substitute.For<IAlertService>();
    private readonly IShare _share = Substitute.For<IShare>();
    private readonly IFilePicker _filePicker = Substitute.For<IFilePicker>();
    private readonly IRecentsPrivacyService _recentsPrivacy = Substitute.For<IRecentsPrivacyService>();
    private readonly IMessenger _messenger = new WeakReferenceMessenger();
    private readonly List<bool> _replacedCallbacks = [];
    private readonly List<DataReplacedMessage> _messages = [];
    private readonly DataManagementViewModel _vm;

    public DataManagementViewModelTests()
    {
        _vm = new DataManagementViewModel(
            _dataService, _alerts, new WriteGuard(_alerts, NullLogger<WriteGuard>.Instance), _share, _filePicker, _recentsPrivacy, _messenger);
        _vm.SetDataReplacedCallback(deleted =>
        {
            _replacedCallbacks.Add(deleted);
            return Task.CompletedTask;
        });
        _messenger.Register<DataReplacedMessage>(this, (_, m) => _messages.Add(m));
    }

    // --- DeleteAll ---

    [Fact]
    public async Task DeleteAll_Confirmed_DeletesAndLeavesTheSettings()
    {
        Confirm(DataManagementViewModel.DeleteTitle, true);

        await _vm.DeleteAllCommand.ExecuteAsync(null);

        await _alerts.Received(1).ConfirmAsync(
            DataManagementViewModel.DeleteTitle, DataManagementViewModel.DeleteMessage, DataManagementViewModel.DeleteAccept, DataManagementViewModel.Cancel);
        await _dataService.Received(1).DeleteAllAsync();
        _recentsPrivacy.Received(1).SetHidden(false);
        _messages.Should().ContainSingle();
        _replacedCallbacks.Should().Equal(true);
    }

    [Fact]
    public void DeleteMessage_NamesEverythingThatGoes()
    {
        DataManagementViewModel.DeleteMessage.Should()
            .Contain("lançamentos").And.Contain("contas e cartões").And.Contain("categorias")
            .And.Contain("nome e foto").And.Contain("PIN");
    }

    [Fact]
    public async Task DeleteAll_Cancelled_DeletesNothing()
    {
        Confirm(DataManagementViewModel.DeleteTitle, false);

        await _vm.DeleteAllCommand.ExecuteAsync(null);

        await _dataService.DidNotReceive().DeleteAllAsync();
        _replacedCallbacks.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAll_Fails_ShowsErrorAndStays()
    {
        Confirm(DataManagementViewModel.DeleteTitle, true);
        _dataService.DeleteAllAsync().ThrowsAsync(new IOException());

        await _vm.DeleteAllCommand.ExecuteAsync(null);

        await _alerts.Received(1).ShowAsync(WriteGuard.ErrorTitle, WriteGuard.ErrorMessage);
        _messages.Should().BeEmpty();
        _replacedCallbacks.Should().BeEmpty();
    }

    // --- Export ---

    [Fact]
    public async Task Export_SharesTheBackupAndTheSpreadsheet()
    {
        _dataService.ExportAsync().Returns(new[] { "/cache/export/mensal-backup.json", "/cache/export/mensal-lancamentos.csv" });

        await _vm.ExportCommand.ExecuteAsync(null);

        await _share.Received(1).RequestAsync(Arg.Is<ShareMultipleFilesRequest>(r =>
            r.Files!.Select(f => f.FullPath).SequenceEqual(new[] { "/cache/export/mensal-backup.json", "/cache/export/mensal-lancamentos.csv" })));
    }

    [Fact]
    public async Task Export_Fails_SharesNothing()
    {
        _dataService.ExportAsync().ThrowsAsync(new IOException());

        await _vm.ExportCommand.ExecuteAsync(null);

        await _share.DidNotReceive().RequestAsync(Arg.Any<ShareMultipleFilesRequest>());
        await _alerts.Received(1).ShowAsync(WriteGuard.ErrorTitle, WriteGuard.ErrorMessage);
    }

    // --- Import ---

    [Fact]
    public async Task Import_NoFilePicked_AsksNothing()
    {
        _filePicker.PickAsync(Arg.Any<PickOptions>()).Returns((FileResult?)null);

        await _vm.ImportCommand.ExecuteAsync(null);

        await _alerts.DidNotReceiveWithAnyArgs().ConfirmAsync(default!, default!, default!, default!);
        await _dataService.DidNotReceiveWithAnyArgs().ImportAsync(default!);
    }

    [Fact]
    public async Task Import_Confirmed_ImportsThePickedFileAndLeavesTheSettings()
    {
        PickFile("{\"format\":\"mensal-backup\"}");
        Confirm(DataManagementViewModel.ImportTitle, true);
        string? imported = null;
        _dataService.ImportAsync(Arg.Do<Stream>(s => imported = new StreamReader(s).ReadToEnd())).Returns(Task.CompletedTask);

        await _vm.ImportCommand.ExecuteAsync(null);

        imported.Should().Be("{\"format\":\"mensal-backup\"}");
        await _alerts.Received(1).ConfirmAsync(
            DataManagementViewModel.ImportTitle, Arg.Is<string>(m => m.Contains("mensal-backup-2026-11-07.json")),
            DataManagementViewModel.ImportAccept, DataManagementViewModel.Cancel);
        _messages.Should().ContainSingle();
        _replacedCallbacks.Should().Equal(false);
        _recentsPrivacy.DidNotReceiveWithAnyArgs().SetHidden(default);
    }

    [Fact]
    public async Task Import_Cancelled_ImportsNothing()
    {
        PickFile("{}");
        Confirm(DataManagementViewModel.ImportTitle, false);

        await _vm.ImportCommand.ExecuteAsync(null);

        await _dataService.DidNotReceiveWithAnyArgs().ImportAsync(default!);
        _replacedCallbacks.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, DataManagementViewModel.InvalidBackupMessage)]
    [InlineData(true, DataManagementViewModel.NewerBackupMessage)]
    public async Task Import_InvalidBackup_SaysWhyAndStays(bool fromNewerVersion, string expectedMessage)
    {
        PickFile("{}");
        Confirm(DataManagementViewModel.ImportTitle, true);
        _dataService.ImportAsync(Arg.Any<Stream>()).ThrowsAsync(new InvalidBackupException("x") { IsFromNewerVersion = fromNewerVersion });

        await _vm.ImportCommand.ExecuteAsync(null);

        await _alerts.Received(1).ShowAsync(DataManagementViewModel.InvalidBackupTitle, expectedMessage);
        await _alerts.DidNotReceive().ShowAsync(WriteGuard.ErrorTitle, Arg.Any<string>());
        _replacedCallbacks.Should().BeEmpty();
    }

    private void Confirm(string title, bool answer) =>
        _alerts.ConfirmAsync(title, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(answer);

    private void PickFile(string content)
    {
        var folder = Directory.CreateTempSubdirectory("financeapp-tests-").FullName;
        var path = Path.Combine(folder, "mensal-backup-2026-11-07.json");
        File.WriteAllText(path, content);
        _filePicker.PickAsync(Arg.Any<PickOptions>()).Returns(new FileResult(path));
    }
}
