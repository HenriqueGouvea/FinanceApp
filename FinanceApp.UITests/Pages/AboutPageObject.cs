using FinanceApp.UITests.Infrastructure;
using OpenQA.Selenium;

namespace FinanceApp.UITests.Pages;

/// <summary>
/// "Sobre o Mensal": the version, the policy and terms links, the contact and the open source notices. The links open
/// the browser and the e-mail app, outside the app, so they aren't followed here.
/// </summary>
public class AboutPageObject
{
    private static readonly By VersionLabel = AppSession.AutomationId("AboutVersionLabel");
    private static readonly By ContactLabel = AppSession.AutomationId("AboutContactLabel");
    private static readonly By NoticesRow = AppSession.AutomationId("AboutNoticesRow");
    private static readonly By NoticesLabel = AppSession.AutomationId("OpenSourceNoticesLabel");

    private readonly AppSession _session;

    public AboutPageObject(AppSession session)
    {
        _session = session;
    }

    public AboutPageObject WaitUntilLoaded()
    {
        _session.WaitForVisible(VersionLabel);
        return this;
    }

    public string VersionText => _session.WaitForVisible(VersionLabel).Text;

    public string ContactText => _session.ScrollTo("AboutContactLabel").Text;

    public bool AreLinksVisible =>
        _session.IsVisible(AppSession.AutomationId("AboutPrivacyRow")) && _session.IsVisible(AppSession.AutomationId("AboutTermsRow"));

    /// <summary>
    /// Opens "Licenças de código aberto" and returns the start of the notices once they are loaded.
    /// </summary>
    public string ReadNotices()
    {
        _session.Tap(NoticesRow);
        _session.WaitFor(() => _session.VisibleTexts(NoticesLabel).Any(t => t.Length > 0), "the notices to load");
        return _session.WaitForVisible(NoticesLabel).Text;
    }
}
