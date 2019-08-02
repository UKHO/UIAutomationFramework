using System;

using UKHO.WebDriverInterface;

using By = OpenQA.Selenium.By;

namespace UKHO.SeleniumDriver
{
    internal static class Utils
    {
        public static By SeleniumSelector(ISelector selector)
        {
            switch (selector.SelectorType)
            {
                case SelectorType.Name:
                    return By.Name(selector.SelectorValue);
                case SelectorType.Id:
                    return By.Id(selector.SelectorValue);
                case SelectorType.ElementType:
                    return By.TagName(selector.SelectorValue);
                case SelectorType.LinkText:
                    return By.LinkText(selector.SelectorValue);
                case SelectorType.CssSelector:
                    return By.CssSelector(selector.SelectorValue);
                case SelectorType.XPath:
                    return By.XPath(selector.SelectorValue);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}