namespace Eplicta.Ipx;

/// <summary>Whether a period is a lower or an upper limit.</summary>
public enum IpxQualifier
{
    [IpxWire("earliest")] Earliest,
    [IpxWire("latest")] Latest
}
