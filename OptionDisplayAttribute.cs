using System;

namespace WarpToFriends;

[AttributeUsage(AttributeTargets.Property)]
public class OptionDisplayAttribute : Attribute
{
    public string Name { get; }
    public int MaxValue { get; } = 100;
    public int MinValue { get; } = 0;
    public bool HostOnly { get; }

    public OptionDisplayAttribute(string name, bool hostOnly = false)
    {
        Name = name;
        HostOnly = hostOnly;
    }

    public OptionDisplayAttribute(string name, int maxValue, int minValue = 0, bool hostOnly = false)
    {
        Name = name;
        MaxValue = maxValue;
        MinValue = minValue;
        HostOnly = hostOnly;
    }
}
