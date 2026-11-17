namespace Eplicta.Ipx;

/// <summary>What a row describes.</summary>
public enum IpxRowKind
{
    [IpxWire("rule")] Rule,
    [IpxWire("process")] Process,
    [IpxWire("record-type")] RecordType,
    [IpxWire("case-type")] CaseType
}
