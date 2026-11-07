using FinanceApp.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FinanceApp.Tests.Services;

public class WriteGuardTests
{
    private readonly IAlertService _alerts = Substitute.For<IAlertService>();
    private readonly WriteGuard _guard;

    public WriteGuardTests()
    {
        _guard = new WriteGuard(_alerts, NullLogger<WriteGuard>.Instance);
    }

    [Fact]
    public async Task TryAsync_WriteSucceeds_ReturnsTrueWithoutAlert()
    {
        var ran = false;

        var result = await _guard.TryAsync(() =>
        {
            ran = true;
            return Task.CompletedTask;
        });

        result.Should().BeTrue();
        ran.Should().BeTrue();
        await _alerts.DidNotReceiveWithAnyArgs().ShowAsync(default!, default!);
    }

    [Fact]
    public async Task TryAsync_WriteThrows_ReturnsFalseAndTellsTheUser()
    {
        var result = await _guard.TryAsync(() => throw new IOException("disk full"));

        result.Should().BeFalse();
        await _alerts.Received(1).ShowAsync("Não foi possível salvar", "Tente de novo.");
    }
}
