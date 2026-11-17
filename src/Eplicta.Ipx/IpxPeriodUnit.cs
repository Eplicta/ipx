namespace Eplicta.Ipx;

/// <summary>The unit of a retention period as the source states it.</summary>
public enum IpxPeriodUnit
{
    [IpxWire("day")] Day,
    [IpxWire("month")] Month,
    [IpxWire("year")] Year
}
