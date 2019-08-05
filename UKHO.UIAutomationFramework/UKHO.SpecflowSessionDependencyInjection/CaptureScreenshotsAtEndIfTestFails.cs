using System;
using System.IO;

using BoDi;

using TechTalk.SpecFlow;

namespace UKHO.SpecflowSessionDependencyInjection
{
    [Binding]
    public class CaptureScreenshotsAtEndIfTestFails
    {
        private readonly IObjectContainer objectContainer;

        public CaptureScreenshotsAtEndIfTestFails(IObjectContainer objectContainer)
        {
            this.objectContainer = objectContainer;
        }

        [AfterScenario]
        public void CaptureScreenshotsOnError()
        {
            if (ScenarioContext.Current?.TestError == null)
                return;
            try
            {
                var session = objectContainer.Resolve<ISession>();
                session.CaptureScreenShot(Path.Combine(Path.GetTempPath(), ScenarioContext.Current.ScenarioInfo.Title + ".png"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not capture screenshot: '{e.Message}'");
            }
        }
    }
}