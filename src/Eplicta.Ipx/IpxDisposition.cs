namespace Eplicta.Ipx;

/// <summary>What happens to the information.</summary>
public enum IpxDisposition
{
    [IpxWire("retain")] Retain,
    [IpxWire("dispose")] Dispose,
    [IpxWire("handover")] Handover,
    [IpxWire("review")] Review,
    [IpxWire("unclear")] Unclear
}
