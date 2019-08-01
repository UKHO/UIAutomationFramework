# UKHO Browser Automation Framework

The automation framework is intended to help automate the browser for automated tests. It's intended to work with SpecFlow, and by default uses Selenium as the web driver.

## Quick Start Guide

 Include the NuGet package in your SpecFlow project:

```powershell
Install-Package UKHO.UIAutomationFramework
```

 You then need to ensure SpecFlow is looking in the framework assembly for bindings. Ensure it’s added to the spec projects App.config:

```xml
<specFlow>
    <stepAssemblies>
        <stepAssembly assembly="UKHO.SpecflowSessionDependencyInjection" />
    </stepAssemblies>
</specFlow>
```

You specify a number of other settings in the app config for the base URL, etc:

```xml
<appSettings>
    <add key="BaseAddress" value="http://localhost:12881/" />
    <add key="Browser" value="Chrome" />
    <add key="ForceUseJsAlertCode" value="true" />
    <add key="ReuseSession" value="false" />
    <add key="websiteBaseAddress" value="http://localhost:12881/" />
    <add key="FirefoxPath" value="C:\Program Files (x86)\Mozilla Firefox\firefox.exe" />
</appSettings>
```

These settings are as follows:

| Setting | Value  |
|---------|--------|
| BaseAddress | This is where the browser will start |
| Browser | This is the browser that the |

You then need to provide a session factory and register it with SpecFlow’s DI:

```C#
    internal class FMSessionFactory : ISessionFactory
    {
        public BaseSession CreateSession(string testTitle)
        {
            var fmSession = new FMSession(testTitle);
            fmSession.WebDriver.WindowSize = new Size(1280, 1024);
            return fmSession;
        }
    }

    [Binding]
    // ReSharper disable once InconsistentNaming
    public class FMWebDriverSupport
    {
        private readonly IObjectContainer objectContainer;

        public FMWebDriverSupport(IObjectContainer objectContainer)
        {
            this.objectContainer = objectContainer;
        }

        [BeforeScenario]
        public void InitializeWebDriver()
        {
            var sessionInstance = new FMSessionFactory();
            objectContainer.RegisterInstanceAs<ISessionFactory>(sessionInstance);
       }

        public static string BaseUrl => ConfigurationManager.AppSettings["BaseAddress"];
    }
```

Once you’ve done this, you should be able to inject ```ISession``` into any page object model and work with it.

e.g. (NB, I’ve copied a subset, compiler will tell you if you need to implement more methods).

```C#
public class RecoverPasswordPage : PageBase
{
    private readonly ISession session;

    public RecoverPasswordPage(ISession session)
        : base(session)
    {
    }

    protected override string PageId => "MasterBody";

    protected override string ExpectedPageTitle => "Recover your password";

    public override void GoTo()
    {
        throw new NotImplementedException();// How to get to the page, e.g. Session.WebDriver.GoToUrl("~/Catalog/Geo"); Usually, not required
    }

    public bool ValidatePasswordPage()
    {
        return IsAt;
    }
}
```
