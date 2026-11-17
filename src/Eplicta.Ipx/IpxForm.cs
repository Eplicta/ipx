namespace Eplicta.Ipx;

/// <summary>A school form.</summary>
public enum IpxForm
{
    [IpxWire("shared")] Shared,
    [IpxWire("preschool")] Preschool,
    [IpxWire("preschool-class")] PreschoolClass,
    [IpxWire("leisure-centre")] LeisureCentre,
    [IpxWire("compulsory-school")] CompulsorySchool,
    [IpxWire("upper-secondary-school")] UpperSecondarySchool,
    [IpxWire("adult-education")] AdultEducation,
    [IpxWire("other-education")] OtherEducation
}
