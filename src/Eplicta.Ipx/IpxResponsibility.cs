using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>A role and what it is responsible for in the plan. Roles only, never personal names.</summary>
public sealed class IpxResponsibility : IpxObject
{
    public IpxResponsibility() : base(new JsonObject())
    {
    }

    public IpxResponsibility(JsonObject node) : base(node)
    {
    }

    public string Role
    {
        get => GetString("role");
        set => SetString("role", value);
    }

    public string Duty
    {
        get => GetString("duty");
        set => SetString("duty", value);
    }
}
