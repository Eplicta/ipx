using System.Text.Json.Nodes;
using Eplicta.Ipx.Validation;

namespace Eplicta.Ipx.Tests;

public class NewerMinorVersionTests
{
    private static IpxValidationResult Validate(Action<JsonObject> change)
    {
        var node = (JsonObject)JsonNode.Parse(TestFiles.Example());
        change(node);
        return IpxValidator.Validate(new IpxDocument(node));
    }

    [Fact]
    public void A_newer_minor_version_may_add_a_property_and_is_valid_with_a_warning()
    {
        var result = Validate(n =>
        {
            n["formatVersion"] = "1.4.0";
            n["rows"][0]["addedInOneFour"] = new JsonObject { ["x"] = 1 };
            n["document"]["alsoNew"] = "value";
        });

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Contains(result.Warnings, w => w.Code == IpxIssueCodes.NewerMinorVersion);
    }

    [Fact]
    public void A_newer_minor_version_may_add_an_enumeration_value_and_is_valid_with_a_warning()
    {
        var result = Validate(n =>
        {
            n["formatVersion"] = "1.1.0";
            n["rows"][0]["retention"][0]["trigger"] = "calendar-year-end";
            n["rows"][0]["registration"] = "registered-elsewhere";
        });

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Contains(result.Warnings, w => w.Code == IpxIssueCodes.NewerMinorVersion);
    }

    [Fact]
    public void A_newer_minor_version_is_still_checked_for_everything_else()
    {
        var result = Validate(n =>
        {
            n["formatVersion"] = "1.2.0";
            n["rows"][0]["retention"][0]["months"] = -3;
        });

        Assert.False(result.IsValid);
        Assert.All(result.Errors, e => Assert.Equal(IpxIssueCodes.Schema, e.Code));
    }

    [Fact]
    public void A_newer_minor_version_still_gets_the_semantic_rules()
    {
        var result = Validate(n =>
        {
            n["formatVersion"] = "1.2.0";
            n["rows"][0]["addedInOneTwo"] = true;
            n["rows"][0]["node"] = "ghost";
        });

        Assert.Contains(result.Errors, e => e.Code == IpxIssueCodes.MissingReference);
    }

    [Fact]
    public void The_current_version_stays_strict()
    {
        var unknownProperty = Validate(n => n["rows"][0]["addedInOneFour"] = 1);
        var unknownValue = Validate(n => n["rows"][0]["retention"][0]["trigger"] = "calendar-year-end");

        Assert.False(unknownProperty.IsValid);
        Assert.False(unknownValue.IsValid);
    }

    [Fact]
    public void A_retain_entry_with_a_period_warns_whether_it_has_months_or_not()
    {
        var withPeriodOnly = Validate(n =>
        {
            var entry = (JsonObject)n["rows"][0]["retention"][0];
            entry["disposition"] = "retain";
            entry.Remove("months");
            entry["period"] = new JsonObject { ["amount"] = 5, ["unit"] = "year" };
        });

        Assert.True(withPeriodOnly.IsValid, string.Join("; ", withPeriodOnly.Errors));
        Assert.Contains(withPeriodOnly.Warnings, w => w.Code == IpxIssueCodes.RetainWithPeriod && w.Path.EndsWith("/period", StringComparison.Ordinal));
    }

    [Fact]
    public void A_node_that_points_at_a_source_that_does_not_exist_is_an_error()
    {
        var result = Validate(n => n["nodes"][0]["source"] = new JsonObject { ["ref"] = "no-such-source", ["page"] = 1 });

        Assert.Contains(result.Errors, e => e.Code == IpxIssueCodes.MissingReference && e.Path == "/nodes/0/source/ref");
    }
}
