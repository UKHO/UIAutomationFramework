using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Support.UI;

using UKHO.WebDriverInterface;

using IWebDriver = UKHO.WebDriverInterface.IWebDriver;

namespace UKHO.SeleniumDriver
{
    public enum Browser
    {
        InternetExplorer,
        Firefox,
        Chrome
    }

    public class SeleniumWebDriver : IWebDriver, IDisposable
    {
        private const int ScriptTimeoutSeconds = 10;
        private const int DownloadToDefaultDownloads = 1;
        private readonly TimeSpan defaultWaitTimeSpan = TimeSpan.FromSeconds(15);
        private readonly OpenQA.Selenium.IWebDriver driver;
        private bool closed;
        private readonly bool useJsAlertCode;

        public SeleniumWebDriver()
        {
            // TODO This is a little ropey for working out where FireFox will download files to, however I don't have any better options atm.
            var pathUser = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            DownloadsDirectory = Path.Combine(pathUser, "Downloads");

            driver = BuildDriver(ConfigurationManager.AppSettings["Browser"]);
            if ("true" == ConfigurationManager.AppSettings["ForceUseJsAlertCode"])
                useJsAlertCode = true;

            ((IJavaScriptExecutor)driver).ExecuteScript("return document.title");
        }

        public void Dispose()
        {
            if (!closed)
                Close();
        }

        public Size WindowSize
        {
            get => driver.Manage().Window.Size;
            set => driver.Manage().Window.Size = value;
        }

        private TimeSpan DefaultPollingInterval => TimeSpan.FromMilliseconds(500);

        public IElement FindElement(ISelector selector)
        {
            return Execute(() => new SeleniumElement(driver, driver.FindElement(Utils.SeleniumSelector(selector))), true);
        }

        public IEnumerable<IElement> FindElements(ISelector selector)
        {
            return Execute(() => driver.FindElements(Utils.SeleniumSelector(selector)).Select(e => new SeleniumElement(driver, e)), true);
        }

        public void GoToUrl(string url)
        {
            Execute(() => driver.Navigate().GoToUrl(url), true);
        }

        public IElement WaitForElement(ISelector selector, TimeSpan? timeout = null, TimeSpan? pollingInterval = null)
        {
            return Execute(() =>
                   {
                       var seleniumSelector = Utils.SeleniumSelector(selector);
                       var wait = new WebDriverWait(new SystemClock(),
                           driver,
                           timeout ?? defaultWaitTimeSpan,
                           pollingInterval ?? DefaultPollingInterval);

                       bool ElementDisplayed(OpenQA.Selenium.IWebDriver condition)
                       {
                           try
                           {
                               var elementToBeDisplayed = driver.FindElement(seleniumSelector);
                               return elementToBeDisplayed.Displayed;
                           }
                           catch (StaleElementReferenceException)
                           {
                               return false;
                           }
                           catch (NoSuchElementException)
                           {
                               return false;
                           }
                       }

                       wait.Until(ElementDisplayed);
                       return FindElements(selector).FirstOrDefault();
                   });
        }

        public void WaitUntil(Predicate<IWebDriver> predicate, TimeSpan? timeout = null, TimeSpan? pollingInterval = null)
        {
            Execute(() =>
            {
                var wait = new WebDriverWait(new SystemClock(),
                    driver,
                    timeout ?? defaultWaitTimeSpan,
                    pollingInterval ?? DefaultPollingInterval);

                wait.Until(d => predicate(this));
            });
        }

        public void WaitForScripts()
        {
            Execute(() =>
                    {
                        driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(ScriptTimeoutSeconds);
                        ((IJavaScriptExecutor)driver).ExecuteScript("return document.title");
                        if (useJsAlertCode)
                        {
                            ((IJavaScriptExecutor)driver).ExecuteScript(
                                                                        "window.AutomationAlertCapture=window.AutomationAlertCapture||function(){window.alert=function(message){window.AutomationAlerts=window.AutomationAlerts||[];window.AutomationAlerts.push(message);};window.confirm=function(message){return true;};return true;}()");
                        }
                    });
        }

        public void Close()
        {
            driver.Quit();
            closed = true;
        }

        public void CaptureScreenShot(string path)
        {
            try
            {
                var takesScreenshot = (ITakesScreenshot)driver;
                var ss = takesScreenshot.GetScreenshot();
                ss.SaveAsFile(path, ScreenshotImageFormat.Png);
            }
            catch (Exception)
            {
                CaptureScreenForWindow(path);
            }
        }

        public string DownloadsDirectory { get; }

        public string DialogText
        {
            get
            {
                if (useJsAlertCode)
                    return ((IJavaScriptExecutor)driver).ExecuteScript("return (window.AutomationAlerts||[]).pop()").ToString();
                var alert = driver.SwitchTo().Alert();
                return alert.Text;
            }
        }

        public void CloseDialog()
        {
            if (!useJsAlertCode)
                driver.SwitchTo().Alert().Accept();
        }

        public object ExecuteJavaScript(string js, params object[] args)
        {
            return ((IJavaScriptExecutor)driver).ExecuteScript(js, args);
        }

        private OpenQA.Selenium.IWebDriver BuildDriver(string browser)
        {
            if (string.IsNullOrEmpty(browser) || !Enum.TryParse(browser, true, out Browser browserEnum))
                return new InternetExplorerDriver();
            switch (browserEnum)
            {
                case Browser.Firefox:
                    return FirefoxDriver();
                case Browser.Chrome:
                    return ChromeDriver();
                case Browser.InternetExplorer:
                    return new InternetExplorerDriver();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private FirefoxDriver FirefoxDriver()
        {
            var firefoxPath = new[]
                              {
                                  Environment.GetEnvironmentVariable("FirefoxPath"),
                                  ConfigurationManager.AppSettings["FirefoxPath"],
                                  @"C:\Program Files\Mozilla Firefox\firefox.exe",
                                  @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe"
                              }.First(
                                      p =>
                                          !string.IsNullOrEmpty(p)
                                          && File.Exists(p));
            var profile = new FirefoxProfile();
            profile.SetPreference("browser.download.useDownloadDir", true);
            profile.SetPreference("browser.download.folderList", DownloadToDefaultDownloads);
            profile.SetPreference("browser.helperApps.alwaysAsk.force", false);
            var mimeTypes = new[]
                            {
                                "application/xml",
                                "text/plain",
                                "application/x-zip-compressed",
                                "application/octet-stream"
                            };
            profile.SetPreference("browser.helperApps.neverAsk.saveToDisk", string.Join(",", mimeTypes));
            var firefoxOptions = new FirefoxOptions
            {
                Profile = profile,
                BrowserExecutableLocation = firefoxPath
            };
            return new FirefoxDriver(firefoxOptions);
        }

        private string GetChromePath()
        {
            var chromePaths = new List<string>(new[]
                                               {
                                                   Environment.GetEnvironmentVariable("ChromePath"),
                                                   ConfigurationManager.AppSettings["ChromePath"],
                                                   @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                                                   @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
                                               });

            if (ConfigurationManager.AppSettings.AllKeys.Contains("AllowChromeBeta")
                && string.Equals("true", ConfigurationManager.AppSettings["AllowChromeBeta"], StringComparison.InvariantCultureIgnoreCase))
            {
                chromePaths.InsertRange(2,
                                        new[]
                                        {
                                            @"C:\Program Files\Google\Chrome Beta\Application\chrome.exe",
                                            @"C:\Program Files (x86)\Google\Chrome Beta\Application\chrome.exe"

                                        });
            }

            return chromePaths.FirstOrDefault(p => !string.IsNullOrEmpty(p) && File.Exists(p));
        }

        private ChromeDriver ChromeDriver()
        {
            var options = new ChromeOptions();
            if (ConfigurationManager.AppSettings.AllKeys.Contains("ChromeOptions"))
            {
                options.AddArguments(ConfigurationManager.AppSettings["ChromeOptions"].Split(';'));
            }
            options.BinaryLocation = GetChromePath();

            if (ConfigurationManager.AppSettings.AllKeys.Contains("ChromeDriverDirectory"))
            {
                var chromeDriverDirectory = ConfigurationManager.AppSettings["ChromeDriverDirectory"];
                return new ChromeDriver(chromeDriverDirectory, options);
            }
            else
            {
                return new ChromeDriver(options);
            }
        }

        ~SeleniumWebDriver()
        {
            Dispose();
        }

        private void Execute(Action action, bool retry = false)
        {
            Execute(() =>
                    {
                        action();
                        return 0;
                    }, retry);
        }

        private T Execute<T>(Func<T> func, bool retry = false)
        {
            try
            {
                return func();
            }
            catch (WebDriverException e)
            {
                Console.Out.WriteLine("Initial call failed with message {0}, retrying...", e.Message);
                if (retry)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(333));
                    return Execute(func);
                }
                if (!"Modal dialog present".Equals(e.Message, StringComparison.InvariantCultureIgnoreCase))
                    throw CaptureScreenShotAndThrow(e);
                var extraMessage = string.Empty;
                try
                {
                    extraMessage = driver.SwitchTo().Alert().Text;
                }
                catch (NoAlertPresentException)
                {
                }
                throw CaptureScreenShotAndThrow(e, extraMessage);
            }
        }

        private WebDriverException CaptureScreenShotAndThrow(WebDriverException e, string extraMessage = null)
        {
            var tempFileName = Path.GetTempFileName();
            File.Delete(tempFileName);
            var fileName0 = $"{tempFileName}_{RationaliseFileName(e.Message)}0.html";
            var fileName1 = $"{tempFileName}_{RationaliseFileName(e.Message)}1.png";
            var fileName2 = $"{tempFileName}_{RationaliseFileName(e.Message)}2.png";
            if (!string.IsNullOrEmpty(extraMessage))
            {
                var fileName3 = $"{tempFileName}_{RationaliseFileName(e.Message)}3.txt";
                File.WriteAllText(fileName3, extraMessage);
            }
            try
            {
                CaptureDom(fileName0);
                CaptureScreenShot(fileName1);
                try
                {
                    CaptureScreenForWindow(fileName2);
                }
                catch (Win32Exception win32E)
                {
                    return new WebDriverException($"{e.Message}\nScreenshots saved at {fileName1}\nFailed to capture desktop screenshot {win32E.Message}", e);
                }
                return new WebDriverException($"{e.Message}\nScreenshots saved at {fileName1} and {fileName2}", e);
            }
            catch (Win32Exception win32E)
            {
                return new WebDriverException($"{e.Message}\nFailed to capture desktop screenshot {win32E.Message}", e);
            }
        }

        private void CaptureDom(string fileName)
        {
            try
            {
                File.WriteAllText(fileName, driver.PageSource);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e);
            }
        }

        private static string RationaliseFileName(string p)
        {
            return Regex.Replace(p, "[^A-Za-z0-9]+", "_").Replace("__", "_");
        }

        private void CaptureScreenForWindow(string path)
        {
            var window = driver.Manage().Window;
            var size = window.Size;
            using (var bitmap = new Bitmap(size.Width, size.Height))
            {
                using (var g = Graphics.FromImage(bitmap))
                    g.CopyFromScreen(new Point(window.Position.X, window.Position.Y), Point.Empty, size);

                bitmap.Save(path, ImageFormat.Png);
            }
        }

        public void AddCookie(ICookie cookie)
        {
            driver.Manage().Cookies.AddCookie(new Cookie(cookie.Name, cookie.Value, cookie.Domain, cookie.Path, cookie.Expiry));
        }

        public IEnumerable<ICookie> AllCookies()
        {
            return driver.Manage().Cookies.AllCookies.Select(c => new SeleniumCookie(c.Name, c.Value, c.Domain, c.Path, c.Expiry));
        }

        public void DeleteAllCookies()
        {
            driver.Manage().Cookies.DeleteAllCookies();
        }
    }
}