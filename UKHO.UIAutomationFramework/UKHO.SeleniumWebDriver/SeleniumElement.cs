using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using UKHO.WebDriverInterface;
using By = OpenQA.Selenium.By;
using IWebDriver = OpenQA.Selenium.IWebDriver;

namespace UKHO.SeleniumDriver
{
    public class SeleniumElement : IElement
    {
        private readonly IWebElement element;
        private readonly IWebDriver webDriver;

        public SeleniumElement(IWebDriver webDriver, IWebElement element)
        {
            this.webDriver = webDriver;
            this.element = element;
        }

        private TimeSpan DefaultPollingInterval => TimeSpan.FromMilliseconds(500);
        private TimeSpan DefaultWaitTimeSpan => TimeSpan.FromSeconds(15);

        public void SendKeys(string keys)
        {
            element.SendKeys(keys);
        }

        public IElement FindElement(ISelector selector)
        {
            return new SeleniumElement(webDriver, element.FindElement(Utils.SeleniumSelector(selector)));
        }

        public IEnumerable<IElement> FindElements(ISelector selector)
        {
            return element.FindElements(Utils.SeleniumSelector(selector))
                .Select(e => new SeleniumElement(webDriver, e));
        }

        public IElement WaitForElement(ISelector selector, TimeSpan? timeout = null, TimeSpan? pollingInterval = null)
        {
            var wait = new WebDriverWait(new SystemClock(),
                webDriver,
                timeout ?? TimeSpan.FromSeconds(5),
                pollingInterval ?? DefaultPollingInterval);

            wait.Until(d => FindElements(selector).Any());
            return FindElements(selector).FirstOrDefault();
        }

        public void Click()
        {
            MoveTo();

            TryActionWithRetryOnException<ElementClickInterceptedException>(() => element.Click());
        }

        private void TryActionWithRetryOnException<TException>(Action action) where TException : Exception
        {
            var wait = new WebDriverWait(new SystemClock(),
                webDriver,
                DefaultWaitTimeSpan,
                DefaultPollingInterval);

            wait.Until(driver =>
            {
                try
                {
                    action.Invoke();
                    return true;
                }
                catch (TException)
                {
                    return false;
                }
            });
        }

        public void DoubleClick()
        {
            if (IsJqgrid())
            {
                PerformDoubleClickWorkaroundForJqgridInChrome78();
            }
            else
            {
                var action = new Actions(webDriver);
                action.DoubleClick(element);
                action.Perform();
            }
        }

        private bool IsJqgrid()
        {
            return element.GetAttribute("aria-describedby")?.StartsWith("jqg_") == true ||
                   HasClass("jqgrow");
        }

        private void PerformDoubleClickWorkaroundForJqgridInChrome78()
        {
            webDriver.ExecuteJavaScript(@"var clickEvent  = document.createEvent ('MouseEvents');
                                            clickEvent.initEvent ('dblclick', true, true);
                                            arguments[0].dispatchEvent (clickEvent);", element);
        }

        public string GetAttribute(string attributeName)
        {
            var attribute = element.GetAttribute(attributeName);
            return attribute;
        }

        private void MoveTo()
        {
            if (!string.Equals(ElementType, "Option", StringComparison.InvariantCultureIgnoreCase))
            {
                var action = new Actions(webDriver);
                action.MoveToElement(element);
                action.Perform();
            }
        }

        public void MouseOver()
        {
            const string JavaScript = "var evObj = document.createEvent('MouseEvents');" +
                                      "evObj.initMouseEvent(\"mouseover\",true, false, window, 0, 0, 0, 0, 0, false, false, false, false, 0, null);" +
                                      "arguments[0].dispatchEvent(evObj);";
            var js = (IJavaScriptExecutor) webDriver;
            js.ExecuteScript(JavaScript, element);
        }

        public string Text => element.Text;

        public string Value
        {
            get => element.GetAttribute("value");
            set
            {
                Click();
                element.Clear();
                element.SendKeys(value);
            }
        }

        public string ElementType => element.TagName;

        public IElement ParentElement
        {
            get
            {
                if (element.TagName == "body")
                    return null;
                return new SeleniumElement(webDriver, element.FindElement(By.XPath("..")));
            }
        }

        public void SetUploadPath(string path)
        {
            element.SendKeys(path);
        }

        public bool Checked
        {
            get
            {
                var checkedAttribute = element.GetAttribute("checked");
                return bool.Parse(checkedAttribute ?? bool.FalseString);
            }
            set
            {
                if (Checked != value)
                    element.Click();
            }
        }

        public void SetSelectedOptionByValue(string value, bool clearMultiselect = true)
        {
            var selectElement = new SelectElement(element);
            if (clearMultiselect && selectElement.IsMultiple)
                selectElement.DeselectAll();
            selectElement.SelectByValue(value);
        }

        public bool Displayed => element.Displayed;

        public bool Enabled => element.Enabled;

        public bool HasClass(string className)
        {
            var classes = GetAttribute("class");
            return !string.IsNullOrWhiteSpace(classes) && classes.Contains(className);
        }
    }
}