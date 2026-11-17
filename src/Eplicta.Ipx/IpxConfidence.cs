namespace Eplicta.Ipx;

/// <summary>How sure the reader of a source is of a normalisation.</summary>
public enum IpxConfidence
{
    [IpxWire("high")] High,
    [IpxWire("medium")] Medium,
    [IpxWire("low")] Low
}
