namespace Eplicta.Ipx;

/// <summary>Which outcome of a case a retention entry applies to.</summary>
public enum IpxOutcome
{
    [IpxWire("grant")] Grant,
    [IpxWire("refusal")] Refusal
}
