using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>A code in another classification scheme, such as KLASSA.</summary>
public sealed class IpxReference : IpxObject
{
    public IpxReference() : base(new JsonObject())
    {
    }

    public IpxReference(JsonObject node) : base(node)
    {
    }

    public string Scheme
    {
        get => GetString("scheme");
        set => SetString("scheme", value);
    }

    public string Code
    {
        get => GetString("code");
        set => SetString("code", value);
    }

    public string Title
    {
        get => GetString("title");
        set => SetString("title", value);
    }
}
