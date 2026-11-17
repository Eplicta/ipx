namespace Eplicta.Ipx;

/// <summary>What a row describes.</summary>
public enum IpxRowKind
{
    [IpxWire("rule")] Rule,
    [IpxWire("process")] Process,
    [IpxWire("recordType")] RecordType,
    [IpxWire("caseType")] CaseType
}
