using System;
using System.Collections.Generic;
using System.Drawing;

namespace UKHO.WebDriverInterface
{
    public interface IWebDriver : IFindElements
    {
        string DownloadsDirectory { get; }
        string DialogText { get; }
        Size WindowSize { get; set; }

        void GoToUrl(string baseAddress);

        void WaitUntil(Predicate<IWebDriver> predicate, TimeSpan? timeout = null, TimeSpan? pollingInterval = null);

        void Close();

        void CaptureScreenShot(string path);

        void WaitForScripts();

        void CloseDialog();

        object ExecuteJavaScript(string js, params object[] args);

        void AddCookie(ICookie cookie);

        IEnumerable<ICookie> AllCookies();

        void DeleteAllCookies();
    }
}