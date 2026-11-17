namespace Eplicta.Ipx;

/// <summary>The name an enumeration value has in an IPX document.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class IpxWireAttribute : Attribute
{
    public IpxWireAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
