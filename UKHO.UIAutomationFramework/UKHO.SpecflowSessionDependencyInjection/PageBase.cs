using System.Linq;

using UKHO.WebDriverInterface;

namespace UKHO.SpecflowSessionDependencyInjection
{
    public abstract class PageBase
    {
        protected PageBase(ISession session)
        {
            Session = session;
        }

        public ISession Session { get; }

        public virtual string CurrentPageId
        {
            get
            {
                var body = Session.WebDriver.FindElements(By.TagName("body"));
                return body.Single().GetAttribute("id");
            }
        }

        public virtual string PageTitle => Session.WebDriver.FindElements(By.TagName("h1")).First().Text;

        protected string GetValue(string textBoxId)
        {
            var element = Session.WebDriver.FindElement(By.Id(textBoxId));
            return element.Value;
        }

        protected void SetValue(string textBoxId, string value)
        {
            var element = Session.WebDriver.FindElement(By.Id(textBoxId));
            element.Value = value;
        }

        public virtual void WaitUntilLoaded()
        {
            Session.WebDriver.WaitForScripts();
        }
    }
}