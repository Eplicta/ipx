using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>The municipality a plan or a body of rules belongs to.</summary>
public sealed class IpxMunicipality : IpxObject
{
    public IpxMunicipality() : base(new JsonObject())
    {
    }

    public IpxMunicipality(JsonObject node) : base(node)
    {
    }

    /// <summary>The four-digit kommun code.</summary>
    public string Code
    {
        get => GetString("code");
        set => SetString("code", value);
    }

    public string Name
    {
        get => GetString("name");
        set => SetString("name", value);
    }

    public string County
    {
        get => GetString("county");
        set => SetString("county", value);
    }
}
