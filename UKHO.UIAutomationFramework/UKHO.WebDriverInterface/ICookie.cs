using System;

namespace UKHO.WebDriverInterface
{
    public interface ICookie
    {
        string Name { get; }
        string Value { get; }
        string Domain { get; }
        string Path { get; }
        DateTime? Expiry { get; }
    }
}