namespace UKHO.WebDriverInterface
{
    public interface ISelector
    {
        SelectorType SelectorType { get; }
        string SelectorValue { get; }
    }

    public enum SelectorType
    {
        Name,
        Id,
        ElementType,
        LinkText,
        CssSelector,
        XPath,
    }
}