using FinanceApp.ViewModels;

namespace FinanceApp.Views;

/// <summary>
/// The setup wizard, pushed modally on the first run and from "Refazer configuração inicial" in the settings.
/// </summary>
public partial class SetupPage : ContentPage
{
    private readonly SetupViewModel _viewModel;

    public SetupPage(SetupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // Every step starts at its top, not where the previous (longer) step was left scrolled.
        viewModel.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(SetupViewModel.Step))
                await StepScroll.ScrollToAsync(0, 0, animated: false);
        };
    }

    /// <summary>
    /// <paramref name="onClosed"/> runs once the wizard is finished (true) or skipped (false) and must close the page.
    /// </summary>
    public async Task PrepareAsync(bool isRerun, Func<bool, Task> onClosed)
    {
        await _viewModel.PrepareAsync(isRerun);
        _viewModel.SetClosedCallback(onClosed);
    }

    // Back goes to the previous step. On the first step it closes a wizard opened from the settings, while on the
    // first run it leaves the app, as the home button would, so the setup is still offered next time.
    protected override bool OnBackButtonPressed()
    {
        if (!_viewModel.IsFirstStep)
            _viewModel.BackCommand.Execute(null);
        else if (_viewModel.IsRerun)
            _viewModel.SkipCommand.Execute(null);
        else
        {
#if ANDROID
            Platform.CurrentActivity?.MoveTaskToBack(true);
#endif
        }

        return true;
    }
}
