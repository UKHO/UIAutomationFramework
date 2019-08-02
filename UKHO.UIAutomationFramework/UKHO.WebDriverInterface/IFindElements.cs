using System;
using System.Collections.Generic;

namespace UKHO.WebDriverInterface
{
    public interface IFindElements
    {
        IElement WaitForElement(ISelector selector, TimeSpan? timeout = null);

        IElement FindElement(ISelector selector);

        IEnumerable<IElement> FindElements(ISelector tagName);
    }
}