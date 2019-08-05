using System.Configuration;

using BoDi;

using TechTalk.SpecFlow;

namespace UKHO.SpecflowSessionDependencyInjection
{
    [Binding]
    public class WebDriverSupport
    {
        private readonly IObjectContainer objectContainer;
        private readonly ISessionFactory sessionFactory;
        private readonly bool reuseSession;
        private BaseSession sessionInstance;
        private static BaseSession singletonSessionInstance;

        public WebDriverSupport(IObjectContainer objectContainer, ISessionFactory sessionFactory)
        {
            this.objectContainer = objectContainer;
            this.sessionFactory = sessionFactory;
            reuseSession = ("true".Equals(ConfigurationManager.AppSettings["ReuseSession"]));
        }

        [BeforeScenario]
        public void InitializeWebDriver()
        {
            if (reuseSession)
            {
                if (singletonSessionInstance == null)
                    singletonSessionInstance = sessionFactory.CreateSession(ScenarioContext.Current.ScenarioInfo.Title);
                else
                {
                    singletonSessionInstance.Reset(ScenarioContext.Current.ScenarioInfo.Title);
                }
                objectContainer.RegisterInstanceAs<ISession>(singletonSessionInstance);
            }
            else
            {
                sessionInstance = sessionFactory.CreateSession(ScenarioContext.Current.ScenarioInfo.Title);
                objectContainer.RegisterInstanceAs<ISession>(sessionInstance);
            }
        }

        [AfterScenario]
        public void CleanupWebDriver()
        {
            (reuseSession ? singletonSessionInstance : sessionInstance).AfterScenario(!reuseSession);
        }

        [AfterTestRun]
        public static void CloseBrowserAfterTestRun()
        {
            if (singletonSessionInstance == null || (!"true".Equals(ConfigurationManager.AppSettings["ReuseSession"])))
                return;
            singletonSessionInstance.AfterScenario(true);
            singletonSessionInstance = null;
        }
    }
}