using System;

using UKHO.WebDriverInterface;

namespace UKHO.SeleniumDriver
{
    public class SeleniumCookie : ICookie
    {
        public SeleniumCookie(string name, string value, string domain, string path, DateTime? expiry)
        {
            Name = name;
            Value = value;
            Domain = domain;
            Path = path;
            Expiry = expiry;
        }

        public string Name { get; private set; }
        public string Value { get; private set; }
        public string Domain { get; private set; }
        public string Path { get; private set; }
        public DateTime? Expiry { get; private set; }
    }
}