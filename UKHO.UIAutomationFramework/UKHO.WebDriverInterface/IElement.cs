namespace UKHO.WebDriverInterface
{
    public interface IElement : IFindElements
    {
        string ElementType { get; }
        string Text { get; }
        string Value { get; set; }
        bool Checked { get; set; }
        bool Displayed { get; }
        bool Enabled { get; }

        IElement ParentElement { get; }

        void SetUploadPath(string path);

        void SetSelectedOptionByValue(string value, bool clearMultiselect = true);

        void SendKeys(string keys);

        void Click();

        void DoubleClick();

        string GetAttribute(string attributeName);

        void MouseOver();

        bool HasClass(string className);
    }
}