using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>A body in a municipality that runs one or more school forms.</summary>
public sealed class IpxOperator : IpxObject
{
    public IpxOperator() : base(new JsonObject())
    {
    }

    public IpxOperator(JsonObject node) : base(node)
    {
    }

    public string Body
    {
        get => GetString("body");
        set => SetString("body", value);
    }

    public IpxStringList Forms => GetStringList("forms");

    public string Note
    {
        get => GetString("note");
        set => SetString("note", value);
    }
}
