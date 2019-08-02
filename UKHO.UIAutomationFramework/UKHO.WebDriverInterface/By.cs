namespace UKHO.WebDriverInterface
{
    public static class By
    {
        public static ISelector Name(string selectorValue)
            => new Selector(SelectorType.Name, selectorValue);

        public static ISelector TagName(string selectorValue)
            => new Selector(SelectorType.ElementType, selectorValue);

        public static ISelector Id(string selectorValue)
            => new Selector(SelectorType.Id, selectorValue);

        public static ISelector LinkText(string selectorValue)
            => new Selector(SelectorType.LinkText, selectorValue);

        public static ISelector CssSelector(string selectorValue)
            => new Selector(SelectorType.CssSelector, selectorValue);

        public static ISelector Classname(string classname)
            => new Selector(SelectorType.CssSelector, "." + classname);

        public static ISelector XPath(string selector)
            => new Selector(SelectorType.XPath, selector);
    }

    internal class Selector : ISelector
    {
        public Selector(SelectorType selectorType, string selectorValue)
        {
            SelectorType = selectorType;
            SelectorValue = selectorValue;
        }

        public SelectorType SelectorType { get; }
        public string SelectorValue { get; }
    }
}