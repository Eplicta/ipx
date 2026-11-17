using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>Where the content came from and what is known about it. The sources, the plans, what is missing and the checks are reachable through Node and are kept as they are.</summary>
public sealed class IpxProvenance : IpxObject
{
    public IpxProvenance() : base(new JsonObject())
    {
    }

    public IpxProvenance(JsonObject node) : base(node)
    {
    }

    public IpxList<IpxOrigin> Origins => GetList("origins", node => new IpxOrigin(node));

    public IpxList<IpxConversion> Conversions => GetList("conversions", node => new IpxConversion(node));
}
