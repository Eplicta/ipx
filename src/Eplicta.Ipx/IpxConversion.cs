using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>A conversion a document has been through, and what it could not carry.</summary>
public sealed class IpxConversion : IpxObject
{
    public IpxConversion() : base(new JsonObject())
    {
    }

    public IpxConversion(JsonObject node) : base(node)
    {
    }

    /// <summary>The date of the conversion, yyyy-MM-dd.</summary>
    public string On
    {
        get => GetString("on");
        set => SetString("on", value);
    }

    public string From
    {
        get => GetString("from");
        set => SetString("from", value);
    }

    public string To
    {
        get => GetString("to");
        set => SetString("to", value);
    }

    public string Tool
    {
        get => GetString("tool");
        set => SetString("tool", value);
    }

    public IpxStringList Losses => GetStringList("losses");
}
