using System.Text.Json.Nodes;
using Eplicta.Ipx.Validation;

namespace Eplicta.Ipx.Tests;

public class ModelTests
{
    private static IpxRetention RetentionOf(string json) => new((JsonObject)JsonNode.Parse(json));

    [Fact]
    public void Create_builds_a_document_that_validates()
    {
        var document = IpxDocument.Create(IpxKind.Plan, "11111111-1111-4111-8111-111111111111", IpxText.Create(("sv", "Plan"), ("en", "Plan")), "sv");
        var node = new IpxNode { Id = "n1", Parent = null };
        node.Title = IpxText.Create(("en", "Govern"));
        document.Nodes.Add(node);
        var row = new IpxRow { Id = "r1", NodeId = "n1" };
        row.Forms.Add("shared");
        row.Retention.Add(new IpxRetention { Disposition = IpxDisposition.Retain, Trigger = IpxTrigger.None });
        document.Rows.Add(row);

        var result = IpxValidator.Validate(document);

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.Message)));
        Assert.Equal("ipx", document.Format);
        Assert.Equal(IpxDocument.CurrentVersion, document.FormatVersion);
    }

    [Fact]
    public void Ext_keeps_values_under_a_reverse_domain_key()
    {
        var row = new IpxRow { Id = "r1" };

        row.EnsureExt().Set("org.example.vendor", new JsonObject { ["code"] = 7 });

        Assert.Equal(7, (int)row.Ext.Get("org.example.vendor")["code"]);
        Assert.Contains("org.example.vendor", row.Ext.Keys);
    }

    [Fact]
    public void Lists_are_live_views_of_the_json()
    {
        var document = TestFiles.ExampleDocument();
        var before = document.Rows.Count;

        var row = new IpxRow { Id = "added" };
        document.Rows.Add(row);

        Assert.Equal(before + 1, document.Rows.Count);
        Assert.Equal(before + 1, ((JsonArray)document.Node["rows"]).Count);
        Assert.True(document.Rows.Remove(document.Rows[before]));
        Assert.Equal(before, document.Rows.Count);
    }

    [Fact]
    public void Retention_enums_use_their_wire_names()
    {
        var retention = new IpxRetention { Disposition = IpxDisposition.Dispose, Trigger = IpxTrigger.SchoolYearEnd, Qualifier = IpxQualifier.Earliest, Outcome = IpxOutcome.Refusal };

        Assert.Equal("dispose", (string)retention.Node["disposition"]);
        Assert.Equal("school-year-end", (string)retention.Node["trigger"]);
        Assert.Equal("earliest", (string)retention.Node["qualifier"]);
        Assert.Equal("refusal", (string)retention.Node["outcome"]);
    }

    [Fact]
    public void Retention_null_is_not_the_same_as_absent()
    {
        var withNull = RetentionOf("""{"disposition":"retain","months":null}""");
        var without = RetentionOf("""{"disposition":"retain"}""");

        Assert.Null(withNull.Months);
        Assert.Null(without.Months);
        Assert.True(withNull.IsNull("months"));
        Assert.False(without.IsNull("months"));
        Assert.False(without.Has("months"));
    }

    [Fact]
    public void Retention_setting_null_removes_the_property_and_set_null_writes_a_json_null()
    {
        var retention = RetentionOf("""{"disposition":"dispose","months":24}""");

        retention.Months = null;
        Assert.False(retention.Has("months"));

        retention.SetNull("months");
        Assert.True(retention.IsNull("months"));

        retention.Months = 5;
        Assert.Equal(5, retention.Months);
    }

    [Fact]
    public void Retention_unknown_values_become_neutral_and_the_original_is_kept()
    {
        var retention = RetentionOf("""{"disposition":"future-value","trigger":"future-trigger"}""");

        Assert.Equal(IpxDisposition.Unclear, retention.Disposition);
        Assert.Equal(IpxTrigger.Other, retention.Trigger);
        Assert.Equal("future-value", (string)retention.Node["disposition"]);
        Assert.Equal("future-trigger", (string)retention.Node["trigger"]);
    }

    [Fact]
    public void Retention_period_reads_amount_and_unit()
    {
        var retention = RetentionOf("""{"disposition":"dispose","period":{"amount":2.5,"unit":"year"}}""");

        Assert.Equal(2.5, retention.Period.Amount);
        Assert.Equal(IpxPeriodUnit.Year, retention.Period.Unit);
    }

    [Fact]
    public void Row_defaults_apply_when_the_properties_are_absent()
    {
        var row = new IpxRow { Id = "r1" };

        Assert.Equal(IpxRowKind.Rule, row.RowKind);
        Assert.True(row.Current);
        Assert.Null(row.Registration);
        Assert.Empty(row.Mappings);
    }

    [Fact]
    public void Row_enums_use_their_wire_names()
    {
        var row = new IpxRow { Id = "r1", RowKind = IpxRowKind.CaseType, Registration = IpxRegistration.KeptOrdered, Confidence = IpxConfidence.Low };

        Assert.Equal("case-type", (string)row.Node["rowKind"]);
        Assert.Equal("kept-ordered", (string)row.Node["registration"]);
        Assert.Equal("low", (string)row.Node["confidence"]);
    }

    [Fact]
    public void Text_keeps_each_language()
    {
        var text = IpxText.Create(("sv", "Plan"), ("en", "Plan in English"));

        text["en"] = "Plan, edited";

        Assert.Equal("Plan", text["sv"]);
        Assert.Equal("Plan, edited", text["en"]);
        Assert.Null(text["de"]);
        Assert.Equal(new[] { "sv", "en" }, text.Languages);
    }
}
