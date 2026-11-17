namespace Eplicta.Ipx.Tests;

public class WireTests
{
    [Theory]
    [InlineData(IpxTrigger.FromCreation, "from-creation")]
    [InlineData(IpxTrigger.SchoolYearEnd, "school-year-end")]
    [InlineData(IpxTrigger.None, "none")]
    public void Triggers_have_their_wire_names(IpxTrigger value, string wire)
    {
        Assert.Equal(wire, IpxWire.ToWire(value));
        Assert.True(IpxWire.TryParse<IpxTrigger>(wire, out var parsed));
        Assert.Equal(value, parsed);
    }

    [Fact]
    public void Every_enum_value_has_a_distinct_wire_name_that_parses_back()
    {
        AssertRoundTrip<IpxKind>();
        AssertRoundTrip<IpxDisposition>();
        AssertRoundTrip<IpxTrigger>();
        AssertRoundTrip<IpxRowKind>();
        AssertRoundTrip<IpxRegistration>();
        AssertRoundTrip<IpxPeriodUnit>();
        AssertRoundTrip<IpxRelation>();
        AssertRoundTrip<IpxQualifier>();
        AssertRoundTrip<IpxOutcome>();
        AssertRoundTrip<IpxConfidence>();
        AssertRoundTrip<IpxForm>();
    }

    [Fact]
    public void Unknown_wire_names_do_not_parse()
    {
        Assert.False(IpxWire.TryParse<IpxDisposition>("burn", out _));
        Assert.False(IpxWire.TryParse<IpxDisposition>(null, out _));
        Assert.False(IpxWire.TryParse<IpxDisposition>("Retain", out _));
    }

    private static void AssertRoundTrip<T>() where T : struct, Enum
    {
        var names = new HashSet<string>();
        foreach (var value in Enum.GetValues<T>())
        {
            var wire = IpxWire.ToWire(value);
            Assert.True(names.Add(wire), $"{typeof(T).Name}: {wire} is used twice");
            Assert.True(IpxWire.TryParse<T>(wire, out var parsed));
            Assert.Equal(value, parsed);
        }
    }
}
