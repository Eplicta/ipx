namespace Eplicta.Ipx;

/// <summary>What a retention period counts from.</summary>
public enum IpxTrigger
{
    [IpxWire("from-creation")] FromCreation,
    [IpxWire("year-end")] YearEnd,
    [IpxWire("school-year-end")] SchoolYearEnd,
    [IpxWire("case-closed")] CaseClosed,
    [IpxWire("enrolment-end")] EnrolmentEnd,
    [IpxWire("decision-date")] DecisionDate,
    [IpxWire("superseded")] Superseded,
    [IpxWire("obsolete")] Obsolete,
    [IpxWire("other")] Other,
    [IpxWire("unspecified")] Unspecified,
    [IpxWire("none")] None
}
