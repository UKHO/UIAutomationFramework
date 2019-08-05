using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

using UKHO.SeleniumDriver;
using UKHO.WebDriverInterface;

namespace UKHO.SpecflowSessionDependencyInjection
{
    public interface ISession
    {
        IWebDriver WebDriver { get; }
        string DialogText { get; }

        void CloseDialog();

        void AddCleanupAction(Action action);

        TPage CurrentPage<TPage>() where TPage : PageBase;

        PageBase CurrentPage(Type pageType);

        void Pause(TimeSpan timeSpan);

        void CaptureScreenShot(string path = null);

        T CaptureScreenShot<T>(T exception) where T : Exception;

        /// <summary>
        ///     Executes an action to cause a file to be downloaded and returns the path to the file. The downloaded file will
        ///     be removed as part of the cleanup actions.
        /// </summary>
        /// <param name="action">The action is expected to cause a file to be downloaded.</param>
        /// <returns>Path to the downloaded file that is the result of the action being performed.</returns>
        string DownloadFileAction(Action action);
    }

    public abstract class BaseSession : ISession
    {
        private static readonly object DownloadLock = new object();
        private readonly Queue<Action> cleanupTasks = new Queue<Action>();
        private int screenshotIndex = 1;
        private string sessionTitle;

        private Lazy<IWebDriver> webDriver = new Lazy<IWebDriver>(() => new SeleniumWebDriver(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        public BaseSession(string sessionTitle)
        {
            this.sessionTitle = sessionTitle;
        }

        public static string BaseAddress
        {
            get
            {
                var appSetting = ConfigurationManager.AppSettings["BaseAddress"];
                if (string.IsNullOrWhiteSpace(appSetting))
                    throw new SessionConfigurationException("No BaseAddress has been defined in the App.config.");
                return appSetting;
            }
            set
            {
                ConfigurationManager.AppSettings["BaseAddress"] = value;
            }
        }

        private string DownloadsDirectory
        {
            get
            {
                return WebDriver.DownloadsDirectory;
            }
        }

        internal void AfterScenario(bool close = true)
        {
            ExecuteCleanupTasks();
            if (webDriver == null || !close)
                return;
            WebDriver.Close();
            webDriver = null;
        }

        public void CloseDialog()
        {
            WebDriver.CloseDialog();
        }

        public void AddCleanupAction(Action action)
        {
            cleanupTasks.Enqueue(action);
        }

        public IWebDriver WebDriver
        {
            get
            {
                return webDriver == null ? null : webDriver.Value;
            }
        }

        public string DialogText
        {
            get
            {
                return WebDriver.DialogText;
            }
        }

        public T CurrentPage<T>() where T : PageBase
        {
            return (T)Activator.CreateInstance(typeof(T), this);
        }

        public PageBase CurrentPage(Type pageType)
        {
            if (pageType.IsSubclassOf(typeof(PageBase)))
                return (PageBase)Activator.CreateInstance(pageType, this);
            throw new ArgumentException("pageType is not a PageBase:" + pageType.FullName);
        }

        public void Pause(TimeSpan timeSpan)
        {
            Thread.Sleep(timeSpan);
        }

        public void CaptureScreenShot(string path = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                path = Path.Combine(Path.GetTempPath(), string.Format("{0}_{1}.png", sessionTitle, screenshotIndex++));
            WebDriver.CaptureScreenShot(path);
        }

        public T CaptureScreenShot<T>(T exception) where T : Exception
        {
            var path = Path.Combine(Path.GetTempPath(), string.Format("{0}_{2}_{1}.png", sessionTitle, screenshotIndex++, StripIllegalChars(exception.Message)));
            WebDriver.CaptureScreenShot(path);
            return exception;
        }

        public string DownloadFileAction(Action action)
        {
            lock (DownloadLock)
            {
                var previousContentsOfDownloadsDirectory = Directory.EnumerateFiles(DownloadsDirectory).ToArray();

                action();

                var count = 0;
                while (!Directory.EnumerateFiles(DownloadsDirectory).Except(previousContentsOfDownloadsDirectory).Any() && count++ < 40)
                {
                    Pause(TimeSpan.FromMilliseconds(250));
                }
                try
                {
                    count = 0;
                    while (Directory.EnumerateFiles(DownloadsDirectory).Except(previousContentsOfDownloadsDirectory).Any(f => f.EndsWith(".part") || f.EndsWith(".tmp")) && count < 40)
                    {
                        // Wait for any part files to disapear which indicates the file hasn't finished downloading.
                        Pause(TimeSpan.FromMilliseconds(250));
                    }
                    var downloadedFile = Directory.EnumerateFiles(DownloadsDirectory).Except(previousContentsOfDownloadsDirectory).SingleOrDefault();
                    if (downloadedFile != null)
                    {
                        long lastLength = 0;
                        while ((new FileInfo(downloadedFile).Length > lastLength || lastLength == 0) && count++ < 40)
                        {
                            lastLength = new FileInfo(downloadedFile).Length;
                            Pause(TimeSpan.FromMilliseconds(250));
                        }
                    }
                    else
                        throw new FileNotFoundException("Downloaded file not found.");
                    AddCleanupAction(() =>
                                     {
                                         if (!string.IsNullOrWhiteSpace(downloadedFile) && File.Exists(downloadedFile))
                                             File.Delete(downloadedFile);
                                     });

                    return downloadedFile;
                }
                catch (InvalidOperationException e)
                {
                    throw new InvalidOperationException(
                        string.Format("Error finding the downloaded files: {0}\n Files found: {1}",
                            e.Message,
                            string.Join("\n", Directory.EnumerateFiles(DownloadsDirectory).Except(previousContentsOfDownloadsDirectory))),
                        e);
                }
            }
        }

        public void Reset(string newSessionTitle)
        {
            OnReset();
            ExecuteCleanupTasks();
            this.sessionTitle = newSessionTitle;
        }

        protected virtual void OnReset()
        {
            WebDriver.GoToUrl(BaseAddress);
        }

        private void ExecuteCleanupTasks()
        {
            while (cleanupTasks.Any())
            {
                cleanupTasks.Dequeue()();
            }
        }

        private string StripIllegalChars(string source)
        {
            return Regex.Replace(source, "[^A-Za-z0-9]", "_");
        }
    }

    public class SessionConfigurationException : Exception
    {
        public SessionConfigurationException(string message)
            : base(message)
        {
        }
    }
}