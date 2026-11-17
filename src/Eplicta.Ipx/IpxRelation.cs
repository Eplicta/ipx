namespace Eplicta.Ipx;

/// <summary>How a row corresponds to a node in the taxonomy.</summary>
public enum IpxRelation
{
    [IpxWire("exact")] Exact,
    [IpxWire("covers")] Covers,
    [IpxWire("part")] Part,
    [IpxWire("similar")] Similar
}
