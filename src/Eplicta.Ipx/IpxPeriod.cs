using System.Text.Json.Nodes;

namespace Eplicta.Ipx;

/// <summary>A retention period as the source states it.</summary>
public sealed class IpxPeriod : IpxObject
{
    public IpxPeriod() : base(new JsonObject())
    {
    }

    public IpxPeriod(JsonObject node) : base(node)
    {
    }

    public double? Amount
    {
        get => GetDouble("amount");
        set => SetDouble("amount", value);
    }

    public IpxPeriodUnit? Unit
    {
        get => GetEnumOrNull<IpxPeriodUnit>("unit");
        set => SetEnum("unit", value);
    }
}
