using System;
using System.IO;

using Reqnroll;
using Reqnroll.BoDi;

namespace UKHO.SpecflowSessionDependencyInjection
{
    [Binding]
    public class CaptureScreenshotsAtEndIfTestFails
    {
        private readonly IObjectContainer objectContainer;
        private readonly ScenarioContext scenarioContext;

        public CaptureScreenshotsAtEndIfTestFails(IObjectContainer objectContainer, ScenarioContext scenarioContext)
        {
            this.objectContainer = objectContainer;
            this.scenarioContext = scenarioContext;
        }

        [AfterScenario]
        public void CaptureScreenshotsOnError()
        {
            if (scenarioContext?.TestError == null)
                return;
            try
            {
                var session = objectContainer.Resolve<ISession>();
                session.CaptureScreenShot(Path.Combine(Path.GetTempPath(), scenarioContext.ScenarioInfo.Title + ".png"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not capture screenshot: '{e.Message}'");
            }
        }
    }
}