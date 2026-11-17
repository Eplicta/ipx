namespace Eplicta.Ipx;

/// <summary>How records are registered.</summary>
public enum IpxRegistration
{
    [IpxWire("registered")] Registered,
    [IpxWire("kept-ordered")] KeptOrdered
}
